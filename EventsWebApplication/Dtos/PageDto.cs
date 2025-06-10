namespace EventsWebApplication.Dtos;

public class PageDto<T>
{
    public List<T> Items { get; set; }
    public int TotalItemsCount { get; set; }
    public int PageSize { get; set; }
    public int PagesCount { get; set; }
}