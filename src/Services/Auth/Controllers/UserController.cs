using Microsoft.AspNetCore.Mvc;
using RestApi.Dto.UserDto;
using RestApi.Models;
using RestApi.Services;
using Consul;

namespace RestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IConsulClient _consulClient;
        private readonly UserService _userService;
        public UserController(UserService userService, IConsulClient consulClient)
        {
            _userService = userService;
            _consulClient = consulClient;
        }

        [HttpPost("register")]
        public IActionResult Register(UserRegisterDto userRegisterDto)
        {
            var user = _userService.Register(userRegisterDto);
            return Ok(user);
        }

        

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto)
        {
            //var kvPair = await _consulClient.KV.Get("jwtKey");
            //var obj = kvPair.Response.Value;
            //Console.WriteLine(System.Text.Encoding.UTF8.GetString(obj));

            var userJwtDto = _userService.Login(userLoginDto);
            if (userJwtDto == null)
            {
                return Unauthorized("Email or password is incorrect");
            }
            return Ok(userJwtDto);
        }

    }


}