﻿using ArtcilesServer.DTO;

using ArtcilesServer.Models;

using ArtcilesServer.Repo;
using ArtcilesServer.Services;
using ArticlesServer.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;


namespace ArtcilesServer.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ValidatedControllerBase
    {




        private readonly IMapper _mapper;
        private readonly UserRepo _userRepo;
        private readonly GenericRepository<User> _userAction;
        private readonly GenericRepository<Hobby> _hobbyAction;
        private readonly HashPassword _hashPassword;
        private readonly AuthService _authService;
        public UserController(IMapper mapper,AuthService authService, GenericRepository<Hobby> hobbyActions,GenericRepository<User> userActions,UserRepo userRepo,HashPassword hashPassword)
        {

            _hashPassword = hashPassword;
            _userRepo = userRepo;
            _hobbyAction = hobbyActions;
            _mapper = mapper;
            _userAction = userActions;
            _authService = authService;

        }


        #region Post actions
        [AllowAnonymous]
        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount([FromBody] UserDTO user)
        {
            try
            {
                var User = _mapper.Map<User>(user);

           
                var salt = _hashPassword.GenerateSalt();
                var hashedPassword = _hashPassword.GenerateHash(user.UserHash, salt);

               
                User.UserHash = hashedPassword;
                User.UserSalt = salt; 

   
                await _userAction.AddAsync(User);

                return Ok("User account has been created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred : {ex.Message}");
            }
        }

        [HttpPost("FollowUser")]
        public async Task<IActionResult> FollowUser([FromBody] FollowDTO followDTO)
        {
            var validationResult = ValidateUser(followDTO.UserId);
            if (validationResult != null) return validationResult;

            try
            {


                var follower =await _userAction.GetByIdAsync(followDTO.followerId);
                var followed =await _userAction.GetByIdAsync(followDTO.UserId);

                if(follower == null || followed == null) return NotFound("follower or user not fund");

                await _userRepo.followUser(followDTO);

                return Ok("followed succefuly");

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred : {ex.Message}");
            }
        }
        [AllowAnonymous]
        [HttpPost("AddHobbies")]
        public async Task<IActionResult> AddHobbies([FromBody] HobbiesDTO hobbieDTO)
        {
            try
            {
                var user = await _userAction.GetByIdAsync(hobbieDTO.userId);
                if (user == null) return NotFound("User not found");

                if (hobbieDTO.Hobbies.Count < 3)
                    return BadRequest("At least 3 hobbies required");

                var mappedHobbies = _mapper.Map<List<Hobby>>(hobbieDTO.Hobbies);

               
                await _userRepo.AddHobbies(mappedHobbies, hobbieDTO.userId);

                return Ok("Hobbies added successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred: {ex.Message}");
            }
        }




        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO login)
        {
            try
            {

                var user = await  _userRepo.login(login);
                
                if (user == null) return Unauthorized();

                var token = _authService.GenerateToken(user);
                var refreshToken = _authService.GenerateRefreshToken();
               

                return Ok(new { token,refreshToken });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred : {ex.Message}");
            }
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            var token =  Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
    

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(token)) return Unauthorized("No refresh token provided");

            try
            {
                TokenRequest tokenRequest = new TokenRequest
                {
                    token = token,
                    refreshToken = request.RefreshToken,
                };

                var newtoken = _authService.GenerateNewAccessToken(tokenRequest);
                var newrefreshToken = _authService.GenerateRefreshToken();
                return Ok(new { newtoken ,   newrefreshToken });
            }
            catch (SecurityTokenException ex)
            {
                return Unauthorized("Invalid refresh token");
            }

            
        }





        #endregion
        #region Get actions




        [HttpGet("GetAllAccounts")]
        public async Task<IActionResult> GettAllAccounts()
        {
            try
            {

                var users = await _userAction.GetAllAsync();
                var userlist = _mapper.Map<List<User>>(users);


                if (userlist.Count < 0)
                {
                    return NotFound("No users exist");
                }
                return Ok(userlist);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error : {ex.Message}");
            }

        }
        [AllowAnonymous]
        [HttpGet("GetAllHobbies")]
        public async Task<IActionResult> GetAllHobbies()
        {
            try
            {

                var hobbies = await _hobbyAction.GetAllAsync();
                var hobbylist = _mapper.Map<List<HobbyDTO>>(hobbies);


                if (hobbylist.Count < 0)
                {
                    return NotFound("No hobbies exist");
                }
                return Ok(hobbylist);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error: {ex.Message}");
            }

        }


    


        [HttpGet("RemindUserToAddPicture")]
        public async Task<IActionResult> RemindUserToAddPicture(int userId)
        {

            var validationResult = ValidateUser(userId);
            if (validationResult != null) return validationResult;

            try
            {

                var userHasPFP = await _userRepo.RemindUserToAddProfilePicture(userId);



                
                return Ok(userHasPFP);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error : {ex.Message}");
            }

        }

        [HttpGet("GetAccountById")]
        public async Task<IActionResult> GetUserById([FromQuery]int userId)
        {
            try
            {

                var user = await _userAction.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("No user exist");
                }
                var foundUser = _mapper.Map<User>(user);


                return Ok(foundUser);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An errr: {ex.Message}");
            }

        }

        [HttpGet("SearchAccount")]
        public async Task<IActionResult> GetAccountBySearch([FromQuery] string searchQuery)
        {
            try
            {

                var searchResult = await _userRepo.SearchAsync(searchQuery);

                if(searchResult.Count <= 0)
                {
                   return NotFound("nothing found");
                }

                var foundUsers = _mapper.Map<List<User>>(searchResult);

                return Ok(foundUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error : {ex.Message}");
            }

        }




        [HttpGet("GetUserFollowing")]
        public async Task<IActionResult> GetUserFollowing([FromQuery] int userId)
        {

            try
            {
               
                var Result = await _userRepo.GetUserFollowing(userId);

                if (Result.Count <= 0)
                {
                    return NotFound("nothing found");
                }

                var foundUsers = _mapper.Map<List<FollowerDTO>>(Result);

                return Ok(foundUsers);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error: {ex.Message}");
            }

        }


        [HttpGet("GetUserHobbies")]
        public async Task<IActionResult> GetUserHobbies([FromQuery] int userId)
        {
            try
            {

                var Result = await _userRepo.GetUserHobbies(userId);

                if (Result.Count <= 0)
                {
                    return NotFound("nothing found");
                }

                var foundHobbies = _mapper.Map<List<HobbyDTO>>(Result);

                return Ok(foundHobbies);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error: {ex.Message}");
            }

        }


        #endregion

        [HttpPut("UpdateAccount")]
        public async Task<IActionResult> UpdateAccountData([FromBody] UserDTO user, [FromQuery] int userId)
        {
            var validationResult = ValidateUser(userId);
            if (validationResult != null) return validationResult;

            try
            {

                var selectedUser = await _userAction.GetByIdAsync(userId);

              

                if (selectedUser == null)
                {
                 return NotFound($"user not found.");
                }
               
                _mapper.Map(selectedUser,userId); 
                await _userAction.Update(selectedUser);
                
                return Ok("updated successfully.");
                

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error : {ex.Message}");
            }
        }

        [HttpDelete("DeleteAccount")]
   
        public async Task<IActionResult> DeleteAccount([FromQuery] int userId)
        {

           

            try
            {
                var validationResult = ValidateUser(userId);
                if (validationResult != null) return validationResult;


                var selectedUser = await _userAction.GetByIdAsync(userId);

            

                if (selectedUser == null)
                {
                    return NotFound("user not found.");
                }
                await _userAction.Delete(selectedUser);

                return Ok("deleted successfully.");
            }catch(InvalidOperationException ex)
            {
             
                return Unauthorized("Invalid token.");

            
            }catch(ArgumentException ex)
            {
                return Forbid("You are not authorized to update this account.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error: {ex.Message}");
            }
        }



        [HttpDelete("RemoveFollower")]
        public async Task<IActionResult> RemoveFollower([FromBody] FollowDTO followDTO)
        {
            var validationResult = ValidateUser(followDTO.UserId);
            if (validationResult != null) return validationResult;

            try
            {


                var follower = await _userAction.GetByIdAsync(followDTO.followerId);
                var followed = await _userAction.GetByIdAsync(followDTO.UserId);

                if (follower == null || followed == null) return NotFound("follower or user not fund");

                await _userRepo.removeFollower(followDTO);

                return Ok("followed successfuly");

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred : {ex.Message}");
            }
        }



    }

}
