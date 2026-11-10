using Tumiia.Domain.Analysis;

namespace Tumiia.UnitTests.Analysis;

public class LanguageDetectorTests
{
    [Theory]
    [InlineData("Please find the quote for the bricks and the cement for this site.", "en")]
    [InlineData("Le montant de la facture est pour les travaux et la livraison des briques.", "fr")]
    [InlineData("Die bedrag is vir die stene en dit is nie betaal nie.", "af")]
    [InlineData("Jumla ya malipo ni kwa ajili ya kazi na vifaa vya ujenzi katika eneo hili.", "sw")]
    public void Detect_WhenTheTextHasEnoughCommonWords_FindsTheLanguage(string text, string expected)
    {
        LanguageDetector.Detect(text).ShouldBe(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("R 18 450.00")]
    [InlineData("IMG_3837")]
    public void Detect_WhenThereIsTooLittleText_SaysUndetermined(string text)
    {
        LanguageDetector.Detect(text).ShouldBe(LanguageDetector.Undetermined);
    }
}
