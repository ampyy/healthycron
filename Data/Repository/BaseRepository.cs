using System.Data;
using Dapper;
using HealthyCron.Utilities.Interface;

namespace HealthyCron.Data.Repository
{
    public abstract class BaseRepository
    {
        protected readonly IDbConnectionFactory _connectionFactory;

        protected BaseRepository(IDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        protected async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CommandType? commandType = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryAsync<T>(sql, param, commandType: commandType);
        }

        protected async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CommandType? commandType = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param, commandType: commandType);
        }

        protected async Task<int> ExecuteAsync(string sql, object? param = null, CommandType? commandType = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteAsync(sql, param, commandType: commandType);
        }

        protected async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null, CommandType? commandType = null)
        {
            using var connection = _connectionFactory.CreateConnection();
            return await connection.ExecuteScalarAsync<T>(sql, param, commandType: commandType);
        }
    }
}
