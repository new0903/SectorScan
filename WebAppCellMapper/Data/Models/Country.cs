namespace WebAppCellMapper.Data.Models
{
    public class Country
    {
        public int countryId { get; set; }
        public string name { get; set; }
        public string englishName { get; set; }
        public List<Operator> operators {  get; set; }
    }
}
