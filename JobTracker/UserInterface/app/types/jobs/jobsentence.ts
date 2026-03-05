/* public class JobSentenceDto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string Sentence { get; set; }
    public string? SentenceType { get; set; }
    public float? Score { get; set; }
} */

export interface JobSentenceDto {
    Id: number,
    JobId: number,
    Start: number,
    Length: number,
    Sentence: string,
    SentenceType?: string
    Score?: number;
}