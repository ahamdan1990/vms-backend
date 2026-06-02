using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VisitorManagementSystem.Api.Application.Speech.Interfaces;
using VisitorManagementSystem.Api.Domain.Enums;

namespace VisitorManagementSystem.Api.Infrastructure.Speech;

public class SpeechNormalizationService : ISpeechNormalizationService
{
    private readonly ILogger<SpeechNormalizationService> _logger;

    // Arabic filler words that prefix a spoken answer (e.g. "اسمي علي" → "علي")
    private static readonly string[] _arFillerPrefixes =
    [
        "اسمي", "الاسم", "اسم", "كنيتي", "الكنية", "كنية",
        "جنسيتي", "جنسية", "أنا من", "انا من",
        "شركتي", "الشركة", "شركة", "مؤسسة",
        "رقمي", "رقم هاتفي", "هاتفي", "رقم الهاتف", "رقم",
        "أنا", "انا", "هو", "هي",
    ];

    // Arabic-Indic digit → ASCII digit
    private static readonly (char From, char To)[] _arabicIndic =
    [
        ('٠','0'),('١','1'),('٢','2'),('٣','3'),('٤','4'),
        ('٥','5'),('٦','6'),('٧','7'),('٨','8'),('٩','9'),
    ];

    // Spoken Arabic numbers — MSA + Lebanese/Levantine/Gulf/Iraqi dialects + STT variants.
    // Ordered longest-first (applied in ConvertSpokenNumbers).
    private static readonly (string Word, string Digit)[] _spokenNumbers =
    [
        // 0
        ("صفرة","0"), ("صفر","0"),
        // 1
        ("واحدة","1"), ("واحد","1"), ("إحدى","1"), ("احدى","1"),
        // 2
        ("اثنتين","2"), ("تنتين","2"), ("اثنين","2"), ("اتنين","2"), ("تنين","2"),
        // 3 — MSA + Lebanese (تلاتة/تلات) + STT mis-rendering (دلاتي)
        ("ثلاثة","3"), ("تلاتة","3"), ("ثلاثي","3"), ("تلاتي","3"), ("دلاتي","3"),
        ("تلات","3"), ("ثلاث","3"),
        // 4
        ("أربعتة","4"), ("اربعتة","4"), ("أربعة","4"), ("اربعة","4"),
        ("أربع","4"), ("اربع","4"),
        // 5
        ("خمستة","5"), ("خمسة","5"), ("خمس","5"),
        // 6
        ("ستتة","6"), ("ستة","6"), ("ست","6"),
        // 7
        ("سبعتة","7"), ("سبعة","7"), ("سبع","7"),
        // 8 — MSA + Lebanese (تمانية/تماني/تمان/تمانة)
        ("تمانتة","8"), ("ثمانية","8"), ("تمانية","8"), ("ثماني","8"),
        ("تماني","8"), ("تمانة","8"), ("تمان","8"), ("ثمان","8"),
        // 9
        ("تسعتة","9"), ("تسعة","9"), ("تسع","9"),
    ];

    // Hamza normalisation: أ إ آ → ا
    private static readonly Regex _hamzaRegex = new(@"[أإآ]", RegexOptions.Compiled);
    // Tashkeel (diacritics) removal
    private static readonly Regex _tashkeelRegex = new(@"[ؐ-ًؚ-ٰٟۖ-ۜ۟-۪ۤۧۨ-ۭ]", RegexOptions.Compiled);
    // Collapse multiple spaces
    private static readonly Regex _whitespaceRegex = new(@"\s{2,}", RegexOptions.Compiled);
    // Word-final tanwin fatha (اً) → "ان" — common in Arabic proper names (e.g. حمداً → حمدان).
    // Must run BEFORE RemoveTashkeel so we don't silently drop the represented "n" sound.
    private static readonly Regex _tanwinFathaFinalRegex = new(@"اً(?=\s|$)", RegexOptions.Compiled);

    public SpeechNormalizationService(ILogger<SpeechNormalizationService> logger)
    {
        _logger = logger;
    }

    public Task<string> NormalizeAsync(string rawText, SpeechNormalizationType type, string language, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return Task.FromResult(string.Empty);

        var text = rawText.Trim();

        // Always strip leading filler for Arabic input
        if (language == "ar")
            text = StripArabicFillers(text);

        var normalized = type switch
        {
            SpeechNormalizationType.PersonName   => NormalizePersonName(text, language),
            SpeechNormalizationType.PhoneNumber  => NormalizePhoneNumber(text, language),
            SpeechNormalizationType.CompanyName  => NormalizeCompanyName(text),
            SpeechNormalizationType.Nationality  => NormalizeNationality(text, language),
            SpeechNormalizationType.HostName     => NormalizeHostName(text),
            SpeechNormalizationType.FreeText     => NormalizeFreeText(text),
            SpeechNormalizationType.VehiclePlate => NormalizeVehiclePlate(text),
            SpeechNormalizationType.IdNumber     => NormalizeIdNumber(text),
            SpeechNormalizationType.Email        => NormalizeEmail(text),
            _                                    => text.Trim(),
        };

        _logger.LogDebug("Normalized [{Type}] '{Raw}' → '{Result}'", type, rawText, normalized);
        return Task.FromResult(normalized);
    }

    // ── Private normalizers ───────────────────────────────────────────────────

    private static string NormalizePersonName(string text, string language)
    {
        // Before stripping diacritics, convert word-final tanwin (اً) → "ان"
        // so "حمداً" (pronounced "hamdan") becomes "حمدان" instead of "حمدا".
        text = _tanwinFathaFinalRegex.Replace(text, "ان");
        text = RemoveTashkeel(text);
        if (language == "ar")
            text = NormalizeHamza(text);

        // Title-case each word
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words.Select(w =>
            w.Length > 0 ? char.ToUpper(w[0]) + w[1..].ToLower() : w));
    }

    // Phone-specific STT artifacts: Whisper sometimes transcribes Arabic emphatic consonants
    // with their plain equivalents (ص→س) producing real but wrong words.
    // "سفر" (travel) is the most common mis-transcription of "صفر" (zero) in Levantine dialects.
    private static readonly Regex _phoneSafarZero = new(@"\bسفر\b", RegexOptions.Compiled);

    private static string NormalizePhoneNumber(string text, string language)
    {
        // Convert Arabic-Indic digits
        text = ConvertArabicIndicDigits(text);

        if (language == "ar")
        {
            // Convert spoken Arabic numbers (dialect-aware)
            text = ConvertSpokenNumbers(text);
            // Phone-specific fix: "سفر" is a known STT mis-transcription of "صفر" (zero)
            text = _phoneSafarZero.Replace(text, "0");
        }

        // Keep only digits and +
        var digits = new StringBuilder();
        foreach (var c in text)
            if (char.IsDigit(c) || c == '+') digits.Append(c);

        return digits.ToString();
    }

    private static string NormalizeCompanyName(string text)
        => NormalizeFreeText(text);

    private static string NormalizeNationality(string text, string language)
    {
        text = RemoveTashkeel(text);
        if (language == "ar")
            text = NormalizeHamza(text);
        return text.Trim();
    }

    private static string NormalizeHostName(string text)
        => NormalizeFreeText(text);

    private static string NormalizeFreeText(string text)
        => _whitespaceRegex.Replace(text.Trim(), " ");

    private static string NormalizeVehiclePlate(string text)
    {
        text = ConvertArabicIndicDigits(text);
        return Regex.Replace(text.ToUpperInvariant(), @"[^A-Z0-9]", "");
    }

    private static string NormalizeIdNumber(string text)
    {
        text = ConvertArabicIndicDigits(text);
        return Regex.Replace(text.ToUpperInvariant(), @"[^A-Z0-9]", "");
    }

    private static string NormalizeEmail(string text)
        => text.Trim().ToLowerInvariant();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string StripArabicFillers(string text)
    {
        foreach (var filler in _arFillerPrefixes)
        {
            if (text.StartsWith(filler, StringComparison.OrdinalIgnoreCase))
            {
                text = text[filler.Length..].TrimStart();
                break; // only strip one leading filler
            }
        }
        return text;
    }

    private static string ConvertArabicIndicDigits(string text)
    {
        foreach (var (from, to) in _arabicIndic)
            text = text.Replace(from, to);
        return text;
    }

    private static string ConvertSpokenNumbers(string text)
    {
        // Replace standalone spoken number words with digits (longest match first)
        var ordered = _spokenNumbers.OrderByDescending(n => n.Word.Length);
        foreach (var (word, digit) in ordered)
        {
            var pattern = $@"\b{Regex.Escape(word)}\b";
            text = Regex.Replace(text, pattern, digit);
        }
        return text;
    }

    private static string NormalizeHamza(string text)
        => _hamzaRegex.Replace(text, "ا");

    private static string RemoveTashkeel(string text)
        => _tashkeelRegex.Replace(text, string.Empty);
}
