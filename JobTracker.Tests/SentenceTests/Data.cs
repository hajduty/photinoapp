namespace JobTracker.Tests.SentenceTests;
public class Data
{
    public static readonly string[] RejectionPrototypes = new[]
{
        "Dear X, Thank you for your interest in. After careful review, we have decided to move forward with another candidate. Best regards, Talent Acquisition Team",
        "Unfortunately, we will not be proceeding with your application. We appreciate your interest and encourage you to apply for future positions.",
        "Tyvärr har vi gått vidare med andra sökande. Tack för din ansökan och vi önskar dig lycka till i ditt jobbsökande.",
        "Tack för din ansökan. Efter noggrant övervägande har vi valt att gå vidare med en annan kandidat och kommer inte att kalla in dig för en intervju."
    };

    public static readonly string[] InterviewPrototypes = new[]
    {
        "Dear X, We would like to invite you to an interview to discuss your application further. Please provide your availability. Best regards, Talent Acquisition Team",
        "Hej X, Vi vill gärna bjuda in dig till en intervju för att diskutera din erfarenhet mer i detalj. Vänligen meddela vilka tider som passar. Med vänliga hälsningar, HR-avdelning",
        "Tack för din ansökan. Vi vill gärna ta nästa steg och bjuda in dig till intervju."
    };

    public static readonly string[] OfferPrototypes = new[]
    {
        "Dear X, We are pleased to offer you the position at our company. Your skills stood out and we believe you will be a valuable addition. Best regards, Talent Acquisition Team",
        "Hej X, Vi är glada att kunna erbjuda dig tjänsten hos vårt företag. Vänligen se bifogade dokument med erbjudandedetaljer. Med vänliga hälsningar, HR-avdelning",
        "Tack för din ansökan. Vi är glada att kunna erbjuda dig tjänsten."
    };

    public static readonly string RejectionEmailToClassify = """
    Hej,

    Tack så mycket för din ansökan till vårt. Vi har fått in ett stort antal intressanta ansökningar och efter att ha gått igenom samtliga kan vi nu meddela att vi tyvärr inte kommer att kalla dig till intervju. Detta mot bakgrund av att du inte uppfyller ett av kraven i annonsen, detta krav är:    

    Är på väg att avsluta dina studier eller är nyutexaminerad på kandidat- eller masternivå från universitet eller högskola (inte yrkeshögskola), examen 2025 eller 2026. 

    Har läst en ingenjörsutbildning eller annan relevant teknisk utbildning inom data/systemvetenskap, IT eller liknande.  

    Vi förstår att detta är tråkiga nyheter för dig och vi vill önska dig ett stort lycka till framöver i ditt fortsatta jobbsökande och karriär!

    Vi sparar gärna din ansökan inför framtida möjligheter och hoppas att du vill hålla kontakten med oss.
    Via vårt community kan du ta del av vad som händer hos oss, få inbjudningar till event och tips på lediga jobb som kan passa dig:
    Join our community!

    Är du intresserad av digitalisering och AI? Spana gärna in vår YouTube-serie:
    Det digitala undret

    Följ oss gärna i vardagen via våra kanaler:
    Instagram
    LinkedIn

    Vi önskar dig all lycka till i ditt fortsatta jobbsökande och hoppas att våra vägar korsas igen!
    ​
    Vänliga hälsningar
    """;

    public static readonly string InterviewEmailToClassify = """
    Hej,
    Hoppas allt är bra med dig!
    Jag vill med detta mail bjuda in dig till nästa steg i rekryteringsprocessen som är en digital intervju. 
    Intervjun bygger på kompetensbaserade frågor så vi kommer att fokusera på dina personliga egenskaper. 
    Vi använder Google Meet och intervjun är 30 minuter lång.
    Passar det imorgon kl 13.30?
    Vänligen,
    """;

    public static readonly string OfferEmailToClassify = """
    Hej,

    Vi är glada att kunna erbjuda dig tjänsten hos vårt företag. Din erfarenhet och kompetens imponerade under urvalsprocessen 
    och vi tror att du kommer bli en värdefull tillgång för vårt team.

    Vänligen se bifogade dokument med erbjudandedetaljer och meddela om du har några frågor eller behöver ytterligare information.

    Vi ser fram emot att välkomna dig ombord.

    Med vänliga hälsningar,
    HR-avdelning
    """;
}
