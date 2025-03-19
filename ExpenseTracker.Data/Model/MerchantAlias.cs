public class MerchantAlias
{
    public Guid Id { get; set; }
    public string OriginalName { get; set; }  // Исходное название терминала
    public string NormalizedName { get; set; } // Приведенное к стандартному виду
}