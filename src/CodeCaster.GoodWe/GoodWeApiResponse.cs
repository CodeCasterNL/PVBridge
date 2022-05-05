namespace CodeCaster.GoodWe
{
    public class GoodWeApiResponse
    {
    }
    
    public class GoodWeApiResponse<TResponse> : GoodWeApiResponse
    {
        public TResponse? Data { get; }
        
        public GoodWeApiResponse(TResponse? data)
        {
            Data = data;
        }
    }
}
