using System.Threading.Tasks;

namespace Core.Service
{
    public interface IAlarmService
    {
        Task Start();
        void Stop();
    }
}
