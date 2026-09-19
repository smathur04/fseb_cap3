namespace ReservationService.Models.DTOs;

public class BookInfoDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int AvailableCopies { get; set; }
    public int TotalCopies { get; set; }
}
