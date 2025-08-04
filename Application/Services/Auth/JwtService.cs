using Microsoft.Extensions.Options;

using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;

using System.Security.Claims;

using System.Security.Cryptography;

using System.Text;

using VisitorManagementSystem.Api.Application.Services.Auth;

using VisitorManagementSystem.Api.Application.Services.Configuration;

using VisitorManagementSystem.Api.Domain.Constants;

using VisitorManagementSystem.Api.Domain.Entities;

using VisitorManagementSystem.Api.Domain.Interfaces.Repositories;



namespace VisitorManagementSystem.Api.Application.Services.Auth;



/// <summary>
/// JWT service implementation for token generation and validation
/// </summary>

public class JwtService : IJwtService

{

    private readonly IUnitOfWork _unitOfWork;

    private readonly ILogger<JwtService> _logger;

    private readonly IDynamicConfigurationService _dynamicConfig;

    private readonly JwtSecurityTokenHandler _tokenHandler;



    public JwtService(

        IUnitOfWork unitOfWork,

        ILogger<JwtService> logger,

        IDynamicConfigurationService dynamicConfig)

    {

        _unitOfWork = unitOfWork;

        _logger = logger;

        _dynamicConfig = dynamicConfig;

        _tokenHandler = new JwtSecurityTokenHandler();

    }



    public async Task<JwtTokenResult> GenerateAccessTokenAsync(User user, List<string> permissions,

        Dictionary<string, object>? additionalClaims = null)

    {

        try

        {

            var jwtId = Guid.NewGuid().ToString();

            var issuedAt = DateTime.UtcNow;

            var expiryInMinutes = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "ExpiryInMinutes", 15);
            var expiry = issuedAt.AddMinutes(expiryInMinutes);

            var secretKey = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "SecretKey", "");
            var issuer = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Issuer", "");
            var audience = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Audience", "");
            var algorithm = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Algorithm", "HS256");

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured");
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            // Create claims

            var claims = CreateUserClaims(user, permissions, additionalClaims);

            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, jwtId));

            claims.Add(new Claim(JwtRegisteredClaimNames.Iat,

                new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));



            // Create signing credentials

            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);



            // Create token descriptor

            var tokenDescriptor = new SecurityTokenDescriptor

            {

                Subject = new ClaimsIdentity(claims),

                Expires = expiry,

                Issuer = issuer,

                Audience = audience,

                SigningCredentials = signingCredentials,

                NotBefore = issuedAt

            };



            // Generate token

            var token = _tokenHandler.CreateToken(tokenDescriptor);

            var tokenString = _tokenHandler.WriteToken(token);



            _logger.LogDebug("JWT token generated for user: {UserId}, JTI: {JwtId}", user.Id, jwtId);



            return new JwtTokenResult

            {

                Token = tokenString,

                JwtId = jwtId,

                Expiry = expiry,

                IssuedAt = issuedAt,

                Issuer = issuer,

                Audience = audience,

                Claims = claims

            };

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error generating JWT token for user: {UserId}", user.Id);

            throw;

        }

    }



    public async Task<RefreshToken> GenerateRefreshTokenAsync(User user, string jwtId, string? deviceFingerprint = null,

        string? ipAddress = null, string? userAgent = null)

    {

        try

        {

            var refreshTokenExpiryInDays = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "RefreshTokenExpiryInDays", 7);
            var refreshToken = new RefreshToken

            {

                Token = GenerateSecureToken(),

                JwtId = jwtId,

                UserId = user.Id,

                ExpiryDate = DateTime.UtcNow.AddDays(refreshTokenExpiryInDays),

                CreatedByIp = ipAddress,

                UserAgent = userAgent,

                DeviceFingerprint = deviceFingerprint,

                IsUsed = false,

                IsRevoked = false

            };



            await _unitOfWork.RefreshTokens.AddAsync(refreshToken);

            await _unitOfWork.SaveChangesAsync();



            _logger.LogDebug("Refresh token generated for user: {UserId}, JTI: {JwtId}", user.Id, jwtId);



            return refreshToken;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error generating refresh token for user: {UserId}", user.Id);

            throw;

        }

    }



    public async Task<JwtValidationResult> ValidateTokenAsync(string token, bool validateLifetime = true)

    {

        var result = new JwtValidationResult();



        try

        {

            if (string.IsNullOrEmpty(token))

            {

                result.ErrorMessage = "Token is null or empty";

                return result;

            }

            var secretKey = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "SecretKey", "");
            var issuer = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Issuer", "");
            var audience = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Audience", "");
            var algorithm = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Algorithm", "HS256");
            var validateIssuerSigningKey = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateIssuerSigningKey", true);
            var validateIssuer = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateIssuer", true);
            var validateAudience = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateAudience", true);
            var validateLifetimeConfig = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "ValidateLifetime", true);
            var clockSkewMinutes = await _dynamicConfig.GetConfigurationAsync<int>("JWT", "ClockSkewMinutes", 0);
            var requireExpirationTime = await _dynamicConfig.GetConfigurationAsync<bool>("JWT", "RequireExpirationTime", true);

            if (string.IsNullOrEmpty(secretKey))
            {
                result.ErrorMessage = "JWT SecretKey is not configured";
                return result;
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var validationParameters = new TokenValidationParameters

            {

                ValidateIssuerSigningKey = validateIssuerSigningKey,

                IssuerSigningKey = signingKey,

                ValidateIssuer = validateIssuer,

                ValidIssuer = issuer,

                ValidateAudience = validateAudience,

                ValidAudience = audience,

                ValidateLifetime = validateLifetime && validateLifetimeConfig,

                ClockSkew = TimeSpan.FromMinutes(clockSkewMinutes),

                RequireExpirationTime = requireExpirationTime

            };



            var principal = _tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);



            if (validatedToken is not JwtSecurityToken jwtToken)

            {

                result.ErrorMessage = "Invalid token format";

                return result;

            }



            // Verify algorithm
            var tokenAlgorithm = await _dynamicConfig.GetConfigurationAsync<string>("JWT", "Algorithm", "HS256");
            if (!jwtToken.Header.Alg.Equals(tokenAlgorithm, StringComparison.InvariantCultureIgnoreCase))

            {

                result.ErrorMessage = "Invalid token algorithm";

                return result;

            }



            // Extract claims

            result.Principal = principal;

            result.JwtId = GetClaimValue(principal, JwtRegisteredClaimNames.Jti);

            result.UserEmail = GetClaimValue(principal, ClaimTypes.Email);

            result.SecurityStamp = GetClaimValue(principal, "security_stamp");



            if (int.TryParse(GetClaimValue(principal, ClaimTypes.NameIdentifier), out var userId))

            {

                result.UserId = userId;

            }



            if (long.TryParse(GetClaimValue(principal, JwtRegisteredClaimNames.Exp), out var exp))

            {

                result.Expiry = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;

                result.IsExpired = result.Expiry <= DateTime.UtcNow;

                result.IsNearExpiry = result.Expiry <= DateTime.UtcNow.AddMinutes(5);

            }



            if (long.TryParse(GetClaimValue(principal, JwtRegisteredClaimNames.Iat), out var iat))

            {

                result.IssuedAt = DateTimeOffset.FromUnixTimeSeconds(iat).DateTime;

            }



            // Extract roles and permissions

            result.Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            result.Permissions = new List<string>();



            // Additional validation if user ID is present

            if (result.UserId.HasValue)

            {

                var user = await _unitOfWork.Users.GetByIdAsync(result.UserId.Value);

                if (user == null)

                {

                    result.ErrorMessage = "User not found";

                    return result;

                }



                if (!user.IsValidForAuthentication())

                {

                    result.ErrorMessage = "User account is not valid for authentication";

                    return result;

                }



                // Validate security stamp

                if (!string.IsNullOrEmpty(result.SecurityStamp) && result.SecurityStamp != user.SecurityStamp)

                {

                    result.ErrorMessage = "Security stamp mismatch - token has been invalidated";

                    return result;

                }

            }



            result.IsValid = true;

            return result;

        }

        catch (SecurityTokenExpiredException)

        {

            result.ErrorMessage = "Token has expired";

            result.IsExpired = true;

            return result;

        }

        catch (SecurityTokenInvalidSignatureException)

        {

            result.ErrorMessage = "Invalid token signature";

            return result;

        }

        catch (SecurityTokenValidationException ex)

        {

            result.ErrorMessage = $"Token validation failed: {ex.Message}";

            return result;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Unexpected error validating JWT token");

            result.ErrorMessage = "Token validation failed";

            return result;

        }

    }



    public ClaimsPrincipal? GetClaimsFromToken(string token)

    {

        try

        {

            var jsonToken = _tokenHandler.ReadJwtToken(token);

            var claims = jsonToken.Claims.ToList();

            var identity = new ClaimsIdentity(claims, "jwt");

            return new ClaimsPrincipal(identity);

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error extracting claims from token");

            return null;

        }

    }



    public DateTime? GetTokenExpiration(string token)

    {

        try

        {

            var jsonToken = _tokenHandler.ReadJwtToken(token);

            return jsonToken.ValidTo;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error getting token expiration");

            return null;

        }

    }



    public string? GetJwtId(string token)

    {

        try

        {

            var jsonToken = _tokenHandler.ReadJwtToken(token);

            return jsonToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error getting JWT ID from token");

            return null;

        }

    }



    public int? GetUserId(string token)

    {

        try

        {

            var jsonToken = _tokenHandler.ReadJwtToken(token);

            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : null;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error getting user ID from token");

            return null;

        }

    }



    public string? GetSecurityStamp(string token)

    {

        try

        {

            var jsonToken = _tokenHandler.ReadJwtToken(token);

            return jsonToken.Claims.FirstOrDefault(c => c.Type == "security_stamp")?.Value;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error getting security stamp from token");

            return null;

        }

    }



    public bool IsTokenExpired(string token)

    {

        var expiration = GetTokenExpiration(token);

        return expiration.HasValue && expiration.Value <= DateTime.UtcNow;

    }



    public bool IsTokenNearExpiry(string token, int thresholdMinutes = 5)

    {

        var expiration = GetTokenExpiration(token);

        return expiration.HasValue && expiration.Value <= DateTime.UtcNow.AddMinutes(thresholdMinutes);

    }



    public string GeneratePasswordResetToken(User user, string purpose = "password_reset")

    {

        try

        {

            var claims = new List<Claim>

            {

                new(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new(ClaimTypes.Email, user.Email.Value),

                new("purpose", purpose),

                new("security_stamp", user.SecurityStamp),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString())

            };



            var passwordResetTokenExpiryMinutes = _dynamicConfig.GetConfigurationAsync<int>("JWT", "PasswordResetTokenExpiryMinutes", 30).Result;
            var expiry = DateTime.UtcNow.AddMinutes(passwordResetTokenExpiryMinutes);

            var secretKey = _dynamicConfig.GetConfigurationAsync<string>("JWT", "SecretKey", "").Result;
            var issuer = _dynamicConfig.GetConfigurationAsync<string>("JWT", "Issuer", "").Result;
            var audience = _dynamicConfig.GetConfigurationAsync<string>("JWT", "Audience", "").Result;
            
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured");
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenDescriptor = new SecurityTokenDescriptor

            {

                Subject = new ClaimsIdentity(claims),

                Expires = expiry,

                Issuer = issuer,

                Audience = audience,

                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)

            };



            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return _tokenHandler.WriteToken(token);

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error generating password reset token for user: {UserId}", user.Id);

            throw;

        }

    }



    public bool ValidatePasswordResetToken(string token, User user, string purpose = "password_reset")

    {

        try

        {

            var validationResult = ValidateTokenAsync(token, true).Result;

            if (!validationResult.IsValid || validationResult.Principal == null)

                return false;



            var purposeClaim = GetClaimValue(validationResult.Principal, "purpose");

            if (purposeClaim != purpose)

                return false;



            var userIdClaim = GetClaimValue(validationResult.Principal, ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out var userId) || userId != user.Id)

                return false;



            var securityStamp = GetClaimValue(validationResult.Principal, "security_stamp");

            if (securityStamp != user.SecurityStamp)

                return false;



            return true;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error validating password reset token");

            return false;

        }

    }



    public string GenerateEmailConfirmationToken(User user)

    {

        try

        {

            var claims = new List<Claim>

            {

                new(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new(ClaimTypes.Email, user.Email.Value),

                new("purpose", "email_confirmation"),

                new("security_stamp", user.SecurityStamp),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

            };



            var emailConfirmationTokenExpiryHours = _dynamicConfig.GetConfigurationAsync<int>("JWT", "EmailConfirmationTokenExpiryHours", 24).Result;
            var expiry = DateTime.UtcNow.AddHours(emailConfirmationTokenExpiryHours);

            var secretKey = _dynamicConfig.GetConfigurationAsync<string>("JWT", "SecretKey", "").Result;
            var issuer = _dynamicConfig.GetConfigurationAsync<string>("JWT", "Issuer", "").Result;
            var audience = _dynamicConfig.GetConfigurationAsync<string>("JWT", "Audience", "").Result;
            
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured");
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenDescriptor = new SecurityTokenDescriptor

            {

                Subject = new ClaimsIdentity(claims),

                Expires = expiry,

                Issuer = issuer,

                Audience = audience,

                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)

            };



            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return _tokenHandler.WriteToken(token);

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error generating email confirmation token for user: {UserId}", user.Id);

            throw;

        }

    }



    public bool ValidateEmailConfirmationToken(string token, User user)

    {

        return ValidatePasswordResetToken(token, user, "email_confirmation");

    }



    public string GenerateTwoFactorToken(User user)

    {

        try

        {

            var claims = new List<Claim>

            {

                new(ClaimTypes.NameIdentifier, user.Id.ToString()),

                new("purpose", "two_factor"),

                new("security_stamp", user.SecurityStamp),

                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

            };



            var twoFactorTokenExpiryMinutes = _dynamicConfig.GetConfigurationAsync<int>("JWT", "TwoFactorTokenExpiryMinutes", 5).Result;
            var expiry = DateTime.UtcNow.AddMinutes(twoFactorTokenExpiryMinutes);

            var secretKey = _dynamicConfig.GetConfigurationAsync<string>("JWT", "SecretKey", "").Result;
            var issuer = _dynamicConfig.GetConfigurationAsync<string>("JWT", "Issuer", "").Result;
            var audience = _dynamicConfig.GetConfigurationAsync<string>("JWT", "Audience", "").Result;
            
            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey is not configured");
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenDescriptor = new SecurityTokenDescriptor

            {

                Subject = new ClaimsIdentity(claims),

                Expires = expiry,

                Issuer = issuer,

                Audience = audience,

                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)

            };



            var token = _tokenHandler.CreateToken(tokenDescriptor);

            return _tokenHandler.WriteToken(token);

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error generating two-factor token for user: {UserId}", user.Id);

            throw;

        }

    }



    public bool ValidateTwoFactorToken(string token, User user)

    {

        return ValidatePasswordResetToken(token, user, "two_factor");

    }



    public List<Claim> CreateUserClaims(User user, List<string> permissions, Dictionary<string, object>? additionalClaims = null)

    {

        var claims = new List<Claim>

    {

        new(ClaimTypes.NameIdentifier, user.Id.ToString()),

        new(ClaimTypes.Name, user.FullName),

        new(ClaimTypes.Email, user.Email.Value),

        new(ClaimTypes.Role, user.Role.ToString()),

        new("status", user.Status.ToString()),

        new("security_stamp", user.SecurityStamp),

        new("department", user.Department ?? string.Empty),

        new("job_title", user.JobTitle ?? string.Empty),

        new("employee_id", user.EmployeeId ?? string.Empty),

        new("timezone", user.TimeZone),

        new("language", user.Language),

        new("theme", user.Theme)

    };



        // REMOVED: Don't add permission claims to JWT to reduce token size

        // Permissions will be fetched from database when needed

        /*

        foreach (var permission in permissions)

        {

            claims.Add(new Claim("permission", permission));

        }

        */



        // Add additional claims if provided

        if (additionalClaims != null)

        {

            foreach (var kvp in additionalClaims)

            {

                claims.Add(new Claim(kvp.Key, kvp.Value.ToString() ?? string.Empty));

            }

        }



        return claims;

    }



    public async Task<string> RevokeAllUserTokensAsync(int userId)

    {

        try

        {

            var user = await _unitOfWork.Users.GetByIdAsync(userId);

            if (user == null)

                throw new ArgumentException("User not found", nameof(userId));



            // Update security stamp to invalidate all JWT tokens

            user.UpdateSecurityStamp();

            _unitOfWork.Users.Update(user);

            await _unitOfWork.SaveChangesAsync();



            _logger.LogInformation("All tokens revoked for user: {UserId}", userId);

            return user.SecurityStamp;

        }

        catch (Exception ex)

        {

            _logger.LogError(ex, "Error revoking all tokens for user: {UserId}", userId);

            throw;

        }

    }



    #region Private Methods



    private string GenerateSecureToken()

    {

        var randomBytes = new byte[64];

        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);

    }



    private string? GetClaimValue(ClaimsPrincipal principal, string claimType)

    {

        return principal.FindFirst(claimType)?.Value;

    }



    #endregion

}