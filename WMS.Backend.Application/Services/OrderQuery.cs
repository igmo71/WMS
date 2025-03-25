namespace WMS.Backend.Application.Services
{
    public struct OrderQuery(int? skip = null, int? take = null)
    {
        public int Skip = skip ?? 0;
        public int Take = take ?? 100;
    }
}
