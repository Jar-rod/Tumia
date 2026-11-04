# Frontend integration

How the React app (`apps/web`) talks to the API. This file grows phase by phase; this part covers signing in.
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
