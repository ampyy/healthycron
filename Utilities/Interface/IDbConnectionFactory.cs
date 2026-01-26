using System.Data;

namespace HealthyCron.Utilities.Interface
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
