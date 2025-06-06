namespace Backend.Controllers;

using Backend.Data;
using Backend.Models;
using Backend.Models.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Controller til håndtering af bruger operationer
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(ApplicationDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Hent alle brugere
    /// </summary>
    /// <returns>Liste af alle brugere</returns>
    /// <response code="200">Brugere hentet succesfuldt</response>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(MapToUserDto).ToList();

            _logger.LogInformation("Hentet {Count} brugere totalt", userDtos.Count);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af alle brugere");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af brugere" });
        }
    }

    /// <summary>
    /// Hent brugere som har både Discord og email
    /// </summary>
    /// <returns>Liste af brugere med både Discord og email</returns>
    /// <response code="200">Brugere hentet succesfuldt</response>
    [HttpGet("with-discord-and-email")]
    public async Task<ActionResult<List<UserDto>>> GetUsersWithDiscordAndEmail()
    {
        try
        {
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.DiscordId) && !string.IsNullOrEmpty(u.Email))
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(MapToUserDto).ToList();

            _logger.LogInformation("Hentet {Count} brugere med både Discord og email", userDtos.Count);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af brugere med Discord og email");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af brugere" });
        }
    }

    /// <summary>
    /// Hent brugere som kun har Discord (uden email/password)
    /// </summary>
    /// <returns>Liste af Discord-only brugere</returns>
    /// <response code="200">Brugere hentet succesfuldt</response>
    [HttpGet("discord-only")]
    public async Task<ActionResult<List<UserDto>>> GetDiscordOnlyUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.DiscordId) && string.IsNullOrEmpty(u.Email))
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(MapToUserDto).ToList();

            _logger.LogInformation("Hentet {Count} Discord-only brugere", userDtos.Count);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af Discord-only brugere");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af brugere" });
        }
    }

    /// <summary>
    /// Hent brugere som kun har email (uden Discord)
    /// </summary>
    /// <returns>Liste af email-only brugere</returns>
    /// <response code="200">Brugere hentet succesfuldt</response>
    [HttpGet("email-only")]
    public async Task<ActionResult<List<UserDto>>> GetEmailOnlyUsers()
    {
        try
        {
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Email) && string.IsNullOrEmpty(u.DiscordId))
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(MapToUserDto).ToList();

            _logger.LogInformation("Hentet {Count} email-only brugere", userDtos.Count);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af email-only brugere");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af brugere" });
        }
    }

    /// <summary>
    /// Hent alle brugere som har Discord (med eller uden email)
    /// </summary>
    /// <returns>Liste af brugere med Discord</returns>
    /// <response code="200">Brugere hentet succesfuldt</response>
    [HttpGet("with-discord")]
    public async Task<ActionResult<List<UserDto>>> GetUsersWithDiscord()
    {
        try
        {
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.DiscordId))
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(MapToUserDto).ToList();

            _logger.LogInformation("Hentet {Count} brugere med Discord", userDtos.Count);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af brugere med Discord");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af brugere" });
        }
    }

    /// <summary>
    /// Hent alle brugere som har email (med eller uden Discord)
    /// </summary>
    /// <returns>Liste af brugere med email</returns>
    /// <response code="200">Brugere hentet succesfuldt</response>
    [HttpGet("with-email")]
    public async Task<ActionResult<List<UserDto>>> GetUsersWithEmail()
    {
        try
        {
            var users = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            var userDtos = users.Select(MapToUserDto).ToList();

            _logger.LogInformation("Hentet {Count} brugere med email", userDtos.Count);
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af brugere med email");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af brugere" });
        }
    }

    /// <summary>
    /// Hent bruger statistikker
    /// </summary>
    /// <returns>Statistikker over bruger typer</returns>
    /// <response code="200">Statistikker hentet succesfuldt</response>
    [HttpGet("stats")]
    public async Task<ActionResult<object>> GetUserStats()
    {
        try
        {
            var totalUsers = await _context.Users.CountAsync();
            var discordOnlyUsers = await _context.Users
                .CountAsync(u => !string.IsNullOrEmpty(u.DiscordId) && string.IsNullOrEmpty(u.Email));
            var emailOnlyUsers = await _context.Users
                .CountAsync(u => !string.IsNullOrEmpty(u.Email) && string.IsNullOrEmpty(u.DiscordId));
            var discordAndEmailUsers = await _context.Users
                .CountAsync(u => !string.IsNullOrEmpty(u.DiscordId) && !string.IsNullOrEmpty(u.Email));
            var usersWithDiscord = await _context.Users
                .CountAsync(u => !string.IsNullOrEmpty(u.DiscordId));
            var usersWithEmail = await _context.Users
                .CountAsync(u => !string.IsNullOrEmpty(u.Email));

            var stats = new
            {
                totalUsers,
                discordOnlyUsers,
                emailOnlyUsers,
                discordAndEmailUsers,
                usersWithDiscord,
                usersWithEmail
            };

            _logger.LogInformation("Hentet bruger statistikker: {TotalUsers} brugere totalt", totalUsers);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af bruger statistikker");
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af statistikker" });
        }
    }

    /// <summary>
    /// Hent specifik bruger ved ID
    /// </summary>
    /// <param name="id">Bruger ID</param>
    /// <returns>Bruger information</returns>
    /// <response code="200">Bruger fundet</response>
    /// <response code="404">Bruger ikke fundet</response>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(string id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Bruger ikke fundet" });
            }

            _logger.LogInformation("Hentet bruger {Id}", id);
            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved hentning af bruger {Id}", id);
            return StatusCode(500, new { message = "Der opstod en fejl ved hentning af bruger" });
        }
    }

    /// <summary>
    /// Opdater bruger
    /// </summary>
    /// <param name="id">Bruger ID</param>
    /// <param name="request">Opdatering data</param>
    /// <returns>Opdateret bruger</returns>
    /// <response code="200">Bruger opdateret succesfuldt</response>
    /// <response code="404">Bruger ikke fundet</response>
    /// <response code="400">Ugyldig data</response>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<ActionResult<UserDto>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Bruger ikke fundet" });
            }

            // Opdater felter hvis de er angivet
            if (!string.IsNullOrEmpty(request.Username))
            {
                // Tjek om brugernavn allerede er i brug
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == request.Username && u.Id != id);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Brugernavn er allerede i brug" });
                }
                user.Username = request.Username;
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                // Tjek om email allerede er i brug
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email er allerede i brug" });
                }
                user.Email = request.Email;
            }

            if (request.Level.HasValue)
            {
                user.Level = request.Level.Value;
            }

            if (request.Experience.HasValue)
            {
                user.Experience = request.Experience.Value;
            }

            if (request.Roles != null)
            {
                user.Roles = request.Roles;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            user.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bruger {Id} opdateret", id);
            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved opdatering af bruger {Id}", id);
            return StatusCode(500, new { message = "Der opstod en fejl ved opdatering af bruger" });
        }
    }

    /// <summary>
    /// Slet bruger
    /// </summary>
    /// <param name="id">Bruger ID</param>
    /// <returns>Sletning bekræftelse</returns>
    /// <response code="200">Bruger slettet succesfuldt</response>
    /// <response code="404">Bruger ikke fundet</response>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Bruger ikke fundet" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bruger {Id} slettet", id);
            return Ok(new { message = "Bruger slettet succesfuldt" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved sletning af bruger {Id}", id);
            return StatusCode(500, new { message = "Der opstod en fejl ved sletning af bruger" });
        }
    }

    /// <summary>
    /// Bulk import af brugere fra Active Directory data
    /// </summary>
    /// <param name="request">Bulk import data</param>
    /// <returns>Import resultat</returns>
    /// <response code="200">Import gennemført (kan indeholde fejl)</response>
    /// <response code="400">Ugyldig data</response>
    /// <response code="500">Server fejl</response>
    [HttpPost("bulk-import")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BulkImportResult>> BulkImportUsers([FromBody] BulkImportRequest request)
    {
        var result = new BulkImportResult();
        
        try
        {
            if (request.Users == null || !request.Users.Any())
            {
                return BadRequest(new { message = "Ingen brugere angivet til import" });
            }

            _logger.LogInformation("Starter bulk import af {Count} brugere", request.Users.Count);

            foreach (var userDto in request.Users)
            {
                var importResult = await ImportSingleUser(userDto, request);
                result.Results.Add(importResult);

                if (importResult.Success)
                {
                    if (importResult.Action == "Created")
                        result.SuccessCount++;
                    else if (importResult.Action == "Updated")
                        result.UpdatedCount++;
                }
                else if (importResult.Action == "Skipped")
                {
                    result.SkippedCount++;
                }
                else
                {
                    result.FailedCount++;
                }
            }

            _logger.LogInformation("Bulk import afsluttet: {Success} succesfule, {Failed} fejlede, {Updated} opdaterede, {Skipped} sprunget over", 
                result.SuccessCount, result.FailedCount, result.UpdatedCount, result.SkippedCount);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl under bulk import af brugere");
            result.Errors.Add($"Generel fejl: {ex.Message}");
            return Ok(result); // Returner stadig resultatet, bare med fejl
        }
    }

    /// <summary>
    /// Bulk import fra CSV data
    /// </summary>
    /// <param name="request">CSV import data</param>
    /// <returns>Import resultat</returns>
    /// <response code="200">Import gennemført</response>
    /// <response code="400">Ugyldig CSV data</response>
    [HttpPost("bulk-import-csv")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BulkImportResult>> BulkImportFromCsv([FromBody] CsvBulkImportRequest request)
    {
        try
        {
            var users = ParseCsvToUsers(request.CsvData, request.ColumnMapping);
            
            var bulkRequest = new BulkImportRequest
            {
                Users = users,
                DefaultRoles = request.Settings.DefaultRoles,
                DefaultDepartment = request.Settings.DefaultDepartment,
                UpdateExisting = request.Settings.UpdateExisting,
                SendWelcomeEmails = request.Settings.SendWelcomeEmails
            };

            return await BulkImportUsers(bulkRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved parsing af CSV data");
            return BadRequest(new { message = "Fejl ved parsing af CSV data", error = ex.Message });
        }
    }

    /// <summary>
    /// Hent skabelon til CSV import
    /// </summary>
    /// <returns>CSV skabelon</returns>
    /// <response code="200">Skabelon genereret</response>
    [HttpGet("csv-template")]
    [Authorize(Roles = "Admin,Teacher")]
    public ActionResult GetCsvTemplate()
    {
        var template = "Username,Initial Password,Given Name,Surname,Email,Student ID,Department,Employee Type\n" +
                      "alla437h,C3vk!zRttY8C,Allan Nicolaj i Soylu,Selfoss,allan@example.com,2024001,Datamatiker,Student\n" +
                      "user123,TempPass123!,John,Doe,john@example.com,2024002,Datamatiker,Student";

        return Ok(new { 
            template = template,
            columnMapping = new CsvColumnMapping(),
            instructions = new
            {
                message = "GDPR Information: Kun første navn fra 'Given Name' og første bogstav af 'Surname' gemmes i systemet",
                columns = new
                {
                    username = "Brugernavnet fra Active Directory",
                    initialPassword = "Det midlertidige password",
                    givenName = "Fulde navn - kun første navn gemmes (GDPR)",
                    surname = "Efternavn - kun første bogstav gemmes (GDPR)",
                    email = "Email adresse (valgfri)",
                    studentId = "Student ID (valgfri)",
                    department = "Afdeling/klasse (valgfri)",
                    employeeType = "Student, Teacher, etc. (valgfri)"
                }
            }
        });
    }

    /// <summary>
    /// Validér bulk import data før import
    /// </summary>
    /// <param name="request">Data der skal valideres</param>
    /// <returns>Validerings resultat</returns>
    /// <response code="200">Validering gennemført</response>
    [HttpPost("validate-bulk-import")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ValidateBulkImport([FromBody] BulkImportRequest request)
    {
        var validation = new
        {
            IsValid = true,
            TotalUsers = request.Users?.Count ?? 0,
            Issues = new List<object>(),
            Duplicates = new List<object>(),
            ExistingUsers = new List<object>()
        };

        try
        {
            if (request.Users?.Any() != true)
            {
                return Ok(new { IsValid = false, Issues = new[] { "Ingen brugere angivet" } });
            }

            // Tjek for duplikater i request
            var duplicates = request.Users
                .GroupBy(u => u.Username.ToLower())
                .Where(g => g.Count() > 1)
                .Select(g => new { Username = g.Key, Count = g.Count() })
                .ToList();

            if (duplicates.Any())
            {
                validation.Duplicates.AddRange(duplicates);
            }

            // Tjek for eksisterende brugere i database
            var usernames = request.Users.Select(u => u.Username.ToLower()).ToList();
            var existingUsers = await _context.Users
                .Where(u => usernames.Contains(u.Username.ToLower()))
                .Select(u => new { u.Username, u.Email, u.IsActive })
                .ToListAsync();

            if (existingUsers.Any())
            {
                validation.ExistingUsers.AddRange(existingUsers);
            }

            // Valider individuelle brugere
            var issues = new List<object>();
            foreach (var user in request.Users)
            {
                var userIssues = ValidateUserData(user);
                if (userIssues.Any())
                {
                    issues.Add(new { Username = user.Username, Issues = userIssues });
                }
            }

            return Ok(new
            {
                validation.IsValid,
                validation.TotalUsers,
                Issues = issues,
                validation.Duplicates,
                validation.ExistingUsers,
                Summary = new
                {
                    DuplicatesInRequest = duplicates.Count,
                    ExistingInDatabase = existingUsers.Count,
                    UsersWithIssues = issues.Count,
                    ReadyForImport = request.Users.Count - duplicates.Count - issues.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl under validering af bulk import");
            return StatusCode(500, new { message = "Fejl under validering" });
        }
    }

    // Helper methods
    private async Task<UserImportResult> ImportSingleUser(AdUserImportDto userDto, BulkImportRequest request)
    {
        var result = new UserImportResult
        {
            Username = userDto.Username
        };

        try
        {
            // Tjek om bruger allerede eksisterer
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == userDto.Username.ToLower());

            if (existingUser != null)
            {
                if (!request.UpdateExisting)
                {
                    result.Action = "Skipped";
                    result.Message = "Bruger eksisterer allerede";
                    result.Success = false;
                    return result;
                }

                // Opdater eksisterende bruger
                UpdateUserFromAdData(existingUser, userDto, request);
                existingUser.LastAdSync = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                result.Success = true;
                result.Action = "Updated";
                result.Message = "Bruger opdateret";
                result.UserId = existingUser.Id;
                return result;
            }

            // Opret ny bruger
            var newUser = CreateUserFromAdData(userDto, request);
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            result.Success = true;
            result.Action = "Created";
            result.Message = "Bruger oprettet";
            result.UserId = newUser.Id;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fejl ved import af bruger {Username}", userDto.Username);
            result.Success = false;
            result.Action = "Failed";
            result.Error = ex.Message;
            return result;
        }
    }

    private User CreateUserFromAdData(AdUserImportDto userDto, BulkImportRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Username = userDto.Username,
            Email = userDto.Email ?? "",
            
            // GDPR-sikker håndtering af navn
            FirstName = ExtractFirstName(userDto.GivenName),
            SurnameInitial = ExtractSurnameInitial(userDto.Surname),
            
            // AD specifikke felter
            InitialPassword = userDto.InitialPassword,
            PasswordChanged = false,
            StudentId = userDto.StudentId,
            Department = userDto.Department ?? request.DefaultDepartment,
            EmployeeType = userDto.EmployeeType ?? "Student",
            AdCreatedAt = DateTime.UtcNow,
            LastAdSync = DateTime.UtcNow,
            
            // Basis felter
            Roles = userDto.Roles ?? request.DefaultRoles,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            
            // Hashet initial password
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.InitialPassword)
        };

        return user;
    }

    private void UpdateUserFromAdData(User user, AdUserImportDto userDto, BulkImportRequest request)
    {
        // Opdater kun felter der er angivet og som skal opdateres
        if (!string.IsNullOrEmpty(userDto.Email) && user.Email != userDto.Email)
            user.Email = userDto.Email;

        // GDPR-sikker opdatering af navn
        var newFirstName = ExtractFirstName(userDto.GivenName);
        if (!string.IsNullOrEmpty(newFirstName))
            user.FirstName = newFirstName;

        var newSurnameInitial = ExtractSurnameInitial(userDto.Surname);
        if (!string.IsNullOrEmpty(newSurnameInitial))
            user.SurnameInitial = newSurnameInitial;

        // Opdater AD felter
        user.StudentId = userDto.StudentId ?? user.StudentId;
        user.Department = userDto.Department ?? request.DefaultDepartment ?? user.Department;
        user.EmployeeType = userDto.EmployeeType ?? user.EmployeeType;
        
        user.LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Ekstraherer første navn fra fuldt navn (GDPR-sikker)
    /// F.eks. "Allan Nicolaj i Soylu" -> "Allan"
    /// </summary>
    private static string ExtractFirstName(string givenName)
    {
        if (string.IsNullOrWhiteSpace(givenName))
            return "";

        var parts = givenName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "";
    }

    /// <summary>
    /// Ekstraherer første bogstav af efternavn (GDPR-sikker)
    /// F.eks. "Selfoss" -> "S"
    /// </summary>
    private static string ExtractSurnameInitial(string surname)
    {
        if (string.IsNullOrWhiteSpace(surname))
            return "";

        return surname.Trim().Substring(0, 1).ToUpper();
    }

    private List<AdUserImportDto> ParseCsvToUsers(string csvData, CsvColumnMapping mapping)
    {
        var users = new List<AdUserImportDto>();
        var lines = csvData.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var startIndex = mapping.HasHeader ? 1 : 0;
        
        for (int i = startIndex; i < lines.Length; i++)
        {
            var columns = lines[i].Split(mapping.Delimiter);
            
            if (columns.Length <= Math.Max(mapping.UsernameColumn, mapping.InitialPasswordColumn))
                continue;

            var user = new AdUserImportDto
            {
                Username = GetColumnValue(columns, mapping.UsernameColumn),
                InitialPassword = GetColumnValue(columns, mapping.InitialPasswordColumn),
                GivenName = GetColumnValue(columns, mapping.GivenNameColumn),
                Surname = GetColumnValue(columns, mapping.SurnameColumn),
                Email = GetColumnValue(columns, mapping.EmailColumn),
                StudentId = GetColumnValue(columns, mapping.StudentIdColumn),
                Department = GetColumnValue(columns, mapping.DepartmentColumn),
                EmployeeType = GetColumnValue(columns, mapping.EmployeeTypeColumn)
            };

            if (!string.IsNullOrWhiteSpace(user.Username))
            {
                users.Add(user);
            }
        }

        return users;
    }

    private static string GetColumnValue(string[] columns, int? columnIndex)
    {
        if (!columnIndex.HasValue || columnIndex < 0 || columnIndex >= columns.Length)
            return "";
        
        return columns[columnIndex.Value].Trim().Trim('"');
    }

    private static List<string> ValidateUserData(AdUserImportDto user)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(user.Username))
            issues.Add("Brugernavn er påkrævet");

        if (string.IsNullOrWhiteSpace(user.InitialPassword))
            issues.Add("Initial password er påkrævet");

        if (user.Username?.Length < 3)
            issues.Add("Brugernavn skal være mindst 3 tegn");

        if (user.InitialPassword?.Length < 6)
            issues.Add("Password skal være mindst 6 tegn");

        if (!string.IsNullOrEmpty(user.Email) && !user.Email.Contains("@"))
            issues.Add("Ugyldig email format");

        return issues;
    }

    /// <summary>
    /// Mapper User entity til UserDto (opdateret med AD felter)
    /// </summary>
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            DiscordId = user.DiscordId,
            GlobalName = user.GlobalName,
            AvatarUrl = user.AvatarUrl,
            
            // AD felter
            FirstName = user.FirstName,
            SurnameInitial = user.SurnameInitial,
            PasswordChanged = user.PasswordChanged,
            StudentId = user.StudentId,
            Department = user.Department,
            EmployeeType = user.EmployeeType,
            AdCreatedAt = user.AdCreatedAt,
            LastAdSync = user.LastAdSync,
            
            Experience = user.Experience,
            Level = user.Level,
            Roles = user.Roles,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
} 