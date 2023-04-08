namespace Knowledge.Services.Answers
{
    public interface IAnswersProvider : IAnswersService
    {
        public string GetProviderName();
    }
}
