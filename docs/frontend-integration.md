# Frontend integration

How the React app (`apps/web`) talks to the API. This file grows phase by phase; it covers signing in, uploading and pasting, and reading the analysis.
The full contract is in `services/api/src/Tumiia.Api/openapi/v1.json`. Generate the typed client from it, and
never edit the generated client by hand.

## Calling the API

- Call the API at the **relative** path `/api/v1/...`. In AWS, CloudFront serves the app and the API from one
  address and routes `/api/*` to the API, so there is no CORS and no API host to configure.
- Locally, proxy `/api` to the backend in the Vite config:

  ```ts
  // vite.config.ts
  export default defineConfig({
    server: { proxy: { '/api': 'http://localhost:5194' } },
  });
  ```

- Every error comes back as ProblemDetails JSON (`application/problem+json`) with a `title` to show, and a
  `correlationId` to quote when reporting a problem.

## Signing in: the rules

1. **Keep the access token in memory only.** A module-level variable or React state; never `localStorage`,
   `sessionStorage` or a readable cookie. Send it as `Authorization: Bearer <accessToken>`.
2. **The refresh token is not yours to handle.** It arrives in an HttpOnly cookie (`tumiia_refresh`) that
   JavaScript cannot read. The browser sends it to `/api/v1/auth/*` by itself.
3. **On app load, call `POST /api/v1/auth/refresh`** to restore the session from the cookie. 200 means signed
   in (use the token and user in the body); 401 means show the sign-in screen.
4. **After a 401 from any other call, refresh once, then retry the call once.** If the refresh also answers 401,
   the session has ended: go to the sign-in screen. Do not loop.
5. **Send `credentials: "include"`** (fetch) or `withCredentials: true` (axios) on the auth calls, so the
   cookie goes with them.
6. **Refresh one request at a time.** Each refresh replaces the cookie. If two refreshes race with the same old
   cookie, the API treats the second as a stolen token and ends the session. Share one in-flight refresh promise.

## The auth endpoints

| Call | Body | Success | Notes |
|---|---|---|---|
| `POST /api/v1/auth/register` | `{ email, password, displayName }` | 200 `AuthResponse` + cookie | 400 with `errors.password` / `errors.email`; 409 if the email is taken |
| `POST /api/v1/auth/login` | `{ email, password }` | 200 `AuthResponse` + cookie | 401 "Email or password is incorrect." for every failure, including a locked account |
| `POST /api/v1/auth/refresh` | none (cookie) | 200 `AuthResponse` + new cookie | 401 when the session has ended |
| `POST /api/v1/auth/logout` | none (token + cookie) | 204, cookie cleared | Throw away the in-memory token |
| `POST /api/v1/auth/forgot-password` | `{ email }` | 202 always | Show "If that email has an account, we've sent a link." |
| `POST /api/v1/auth/reset-password` | `{ email, token, newPassword }` | 204 | Read `email` and `token` from the reset page's address; 400 if the link is bad or expired, or `errors.newPassword` |
| `GET /api/v1/auth/me` | none (token) | 200 `CurrentUserResponse` | |

`AuthResponse` is `{ accessToken, expiresAt, user: { id, email, displayName, roles } }`. The access token lasts
15 minutes; the session lasts 14 days from the last refresh.

**Password rules** (show them on the sign-up and reset forms): at least 10 characters, with an upper-case letter,
a lower-case letter, a digit and a symbol.

**The reset link** in the email opens `/reset-password?email=...&token=...` in the app. The page asks for the
new password and calls `reset-password` with the values from the address. The link works for one hour.

## Projects and uploads (summary; the full flow is written up in a later phase)

- `POST /api/v1/projects` `{ name }` creates a vault; `GET /api/v1/projects` lists the user's vaults.
- Uploading is three steps. File bytes never go through the API:
  1. Hash each file with Web Crypto (`crypto.subtle.digest('SHA-256', bytes)`, as lower-case hex) and call
     `POST /api/v1/projects/{projectId}/documents/upload-requests` with
     `{ captureSource: "drag_drop" | "file_picker" | "mobile_upload", files: [{ fileName, contentType, sizeBytes, sha256 }] }`
     (at most 50 files, each at most 50 MB; allowed types: pdf, jpg, jpeg, png, heic, tif, tiff, webp, doc, docx,
     xls, xlsx, csv, txt, dxf, dwg).
  2. For each answer with an `uploadUrl`, `PUT` the file to it **with exactly the same `Content-Type` and size**.
     S3 refuses anything else. The address works for 5 minutes; ask again if it expires. Do not send the
     `Authorization` header to S3.
  3. Call `POST /api/v1/documents/{documentId}/uploaded`. 409 means the file has not arrived or is the wrong size.
- An answer with `existingDocumentId` instead of an `uploadUrl` means that exact file is already in the vault:
  skip the upload and show the existing document.
- `GET /api/v1/projects/{projectId}/documents?status=&category=` lists documents; `GET /api/v1/documents/{id}` reads
  one; `GET /api/v1/documents/{id}/source-url` gives a 5-minute address to open (PDF, JPG, PNG, WebP) or download
  (anything else) the original. Open it in a new tab: the app's content security policy does not allow embedding
  it in a frame.
- A document or project the user cannot access answers 404, exactly like one that does not exist.

## Pasting text or a message

`POST /api/v1/projects/{projectId}/documents/text` with `{ text, title? }` adds pasted text (a copied WhatsApp
message, an instruction, notes) to the vault. The text is stored unchanged as a `.txt` file named after `title`
("Site instruction" becomes `Site instruction.txt`; without a title, `Pasted text 2026-11-10 0930.txt`) and is
analysed like any upload. There is no S3 step.

- **201** with the new `DocumentResponse` (`captureSource: "paste"`, `status: "New"`).
- **200** with the existing document when exactly this text is already in the vault.
- **400** with `errors.text` ("Paste some text first.") when the text is empty or over 200,000 characters.

## After the upload: the analysis

Each document is analysed in the background within seconds. Poll `GET /api/v1/documents/{id}` every 3 seconds
while `document.status` is `New` or `Processing`, and stop when it is anything else:

| `document.status` | Meaning | What to show |
|---|---|---|
| `AutoFiled` | Sure, and not a payment, contract or legal record. Filed. | "Quote · Bricks · R18,450 — Filed ✓" with Undo / Change |
| `AwaitingConfirmation` | Fairly sure, or a payment, contract or legal record (always asked, however sure) | "Looks like a quote from ABC Supplies for bricks — R18,450. Right?" |
| `NeedsReview` | Not sure, or the file could not be read | "I'm not sure about this one." with chips |
| `Failed` | The analysis gave up after 3 tries; `failureReason` says why | Treat like Needs Review |

Once analysed, the response has an `analysis` object (null before). It is the AI's proposal and never changes;
what the user confirms is stored separately later.

- `proposedCategory` (key, e.g. `commercial_quote`) and `proposedCategoryName` ("Quote"), plus `runnerUpCategory`.
- `summary`: one plain line built from the facts found, e.g. "Quote from ABC Supplies for Bricks — R18,450.00,
  12 Oct 2026."
- `classificationConfidence` (0 to 1) and `band` (`High`, `Medium`, `Low`). **Show confidence as words, never as
  a number**: High "Pretty sure", Medium "Fairly sure", Low "Not sure".
- `fields`: one entry per fact. The first is always `document_type`. Others, when found: `party`, `document_date`
  (yyyy-MM-dd), `amount` ("18450.00"), `currency` (ISO code: ZAR, USD, EUR, GBP, ZWG, RWF, KES), `subject` (one of
  the category's chips), `work_package`, `site_reference`, `location` (GPS "lat, long" from a photo), `language`
  (ISO 639-1), and any extra fields a category defines. A fact that was not found is simply absent; nothing is
  guessed. Each field has its own `confidence` and `band`, so one doubtful field (say, the date) can be shown as
  "not sure" without blocking the rest.
- Each field's `evidence` says where it came from: `source` (`bedrock` when the AI read it, `text`, `exif`,
  `classification`), `page`, `line` (typed text only) and a short `snippet` quoting the words. Use it to highlight
  the fact next to the source preview. A snippet ending "(not found in the document's text)" means the AI's quote
  could not be found, so treat that fact as a guess.
- `warnings`: plain sentences worth showing on a Needs Review card ("Old Word (.doc) files can't be read yet...").
- `signals` holds the numbers behind the confidence; it is for debugging, not for display.

## Rate limits

Sign-in calls are limited to 5 a minute per network, and everything to 120 a minute per user. Over the limit
the API answers **429** with a `Retry-After` header (seconds). Show "Too many attempts. Please wait a moment."
and do not retry automatically.

## Local development notes

- Run the API (`dotnet run --project src/Tumiia.Api` in `services/api`) and `npm run dev`; the app is on
  http://localhost:5173.
- No email is sent locally. A reset email is written to a file in `services/api/src/Tumiia.Api/.dev-emails/`;
  open it to get the link.
- The refresh cookie is marked Secure. Chrome, Edge and Firefox accept Secure cookies on `http://localhost`;
  if your browser does not, run the dev server over HTTPS.
