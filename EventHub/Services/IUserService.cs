using EventHub.Models;

namespace EventHub.Services
{
    public interface IUserService
    {
        Task Initialization { get; }
        bool IsInitialized { get; set; }
        void Initailize();
        void Clear();
    }
}
