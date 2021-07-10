﻿using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PetCatalog.Application.Auth;
using PetCatalog.Application.Interfaces;
using PetCatalog.Domain.Interfaces;
using PetCatalog.Domain.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PetCatalog.Application.Services
{
    public class AuthorizationService : IAuthService
    {
        private readonly JWTSettings jwtSettings;
        private readonly IUserRepository userRepository;
        private readonly IRefreshTokenRepository refreshTokenRepository;
        public AuthorizationService(IOptions<JWTSettings> jwtSettings,IUserRepository userRepository,IRefreshTokenRepository refreshTokenRepository)
        {
            this.jwtSettings = jwtSettings.Value;
            this.userRepository = userRepository;
            this.refreshTokenRepository = refreshTokenRepository;
        }

        public UserWithToken Authenticate(User user)
        {
            user = userRepository.Get(user);

            if (user is null)
                return null;

            var userWithToken = new UserWithToken(user);
            var refreshToken = GenerateRefreshToken();
            refreshToken.UserId = user.UserId;
            userWithToken.RefreshToken = refreshToken.Token;
            refreshTokenRepository.Create(refreshToken);

            userWithToken.AccessToken = GenerateAccessToken(user.Email);


            return userWithToken;
        }

        public User GetEmptyUser()
        {
            return new User();
        }

        private string GenerateAccessToken(string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]{
                    new Claim(ClaimTypes.Name,email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(jwtSettings.JwtExpiresIn),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

           return tokenHandler.WriteToken(token);
        }

        private RefreshToken GenerateRefreshToken()
        {
            RefreshToken refreshToken = new RefreshToken();

            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();

            rng.GetBytes(randomNumber);
            refreshToken.Token = Convert.ToBase64String(randomNumber);

            refreshToken.ExpiryDate = DateTime.UtcNow.AddMonths(jwtSettings.RefreshExpiresIn);

            return refreshToken;
        }

        public UserWithToken RefreshToken(RefreshRequest refreshRequest)
        {
            var user = GetUserFromAccessToken(refreshRequest.AccessToken);

            if(user is not null && ValidateRefreshToken(user, refreshRequest.RefreshToken))
            {
                var userWithToken = new UserWithToken(user);
                userWithToken.AccessToken = GenerateAccessToken(user.Email);

                return userWithToken;

            }

            return null;
        }

        private bool ValidateRefreshToken(User user, string refreshToken)
        {
            var refreshTokenUser = refreshTokenRepository.GetRecentTokens(refreshToken).FirstOrDefault();

            if(refreshTokenUser is not null && refreshTokenUser.UserId == user.UserId && refreshTokenUser.ExpiryDate > DateTime.UtcNow)
            {
                return true;
            }

            return false;
        }

        private User GetUserFromAccessToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(jwtSettings.SecretKey);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken securityToken;

            var principle = tokenHandler.ValidateToken(accessToken, tokenValidationParameters,out securityToken);

            var jwtSecurityToken = securityToken as JwtSecurityToken;

            if(jwtSecurityToken is not null && jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,StringComparison.InvariantCultureIgnoreCase))
            {
                var userEmail = principle.FindFirst(ClaimTypes.Name)?.Value;

                return userRepository.Get(userEmail);
            }

            return null;
        }
    }
}
