using server.Domain;
using server.Utils;
using StackExchange.Redis;



namespace server.Middlewares;


public static class AuthenticationMiddlewarePlugin
{
    public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }
}


public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private StackExchange.Redis.IDatabase _redis;


    public AuthenticationMiddleware(RequestDelegate next, IConnectionMultiplexer muxer_redis)
    {
        _redis = muxer_redis.GetDatabase();
        _next = next;
    }


    public async Task Invoke(HttpContext context)
    {
        // Extrai as informações do contexto
        var authorization = context.Request.Headers.Where(x => x.Key == "Authorization").FirstOrDefault().Value.ToString();
        if(authorization != string.Empty){

            var token = authorization.Split(" ")[1];

            string json = _redis.StringGet(token);
            if(json is null){
                //o token não foi localizado no cache. Pode até ser que usuario tenha feito login com sucesso no passsado, mas o token expirou
                throw new ExpectedException("Token de autenticação ausente ou inválido...", System.Net.HttpStatusCode.Unauthorized);
            }

            //injeta o User nos Items
            User user = User.Deserialize(json);
            context.Items["User"] = user;

            //Adia o vencimento do token para caso o usuario use constantemente, a sessão dele não caia
            TimeSpan timeSpanUntilExpiration = DateTime.UtcNow.AddDays(30) - DateTime.UtcNow; 
            _redis.StringSetAsync(token, user.GetJsonSerialized(), timeSpanUntilExpiration);
        }
        
        

        // Chama o próximo middleware na cadeia
        await _next(context);
    }




    


}
