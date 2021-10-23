using System;

namespace CSharpSamples
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
    }

    public class UserNotFoundException : Exception
    {
        public int MissingUserId { get; }

        public UserNotFoundException(int missingUserId) : base($"User {missingUserId} not found")
        {
            MissingUserId = missingUserId;
        }

        public UserNotFoundException(int missingUserId, Exception innerException)
            : base($"User {missingUserId} not found", innerException)
        {
            MissingUserId = missingUserId;
        }
    }

    public interface IUsernameLowerer
    {
        string LowerUsername(User user);
    }

    public class UsernameLowerer : IUsernameLowerer
    {
        public string LowerUsername(User user) => user.Username.ToLower();
    }

    public class UserHelper
    {
        private readonly IUsernameLowerer _usernameLowerer;

        public UserHelper(IUsernameLowerer usernameLowerer)
        {
            _usernameLowerer = usernameLowerer;
        }

        public User? TryGetUser(int id) => new User { Id = id, Username = "SomeUser" };

        public string GetUserNameLowercase(int id)
        {
            User? user = TryGetUser(id);

            if (user is null)
            {
                throw new UserNotFoundException(id);
            }

            return _usernameLowerer.LowerUsername(user);
        }
    }

    public class Program
    {
        public static void Main()
        {
            int userId = 420;

            var helper = new UserHelper(new UsernameLowerer()); // this would come from DI container

            try
            {
                var userNameLowercase = helper.GetUserNameLowercase(userId);
                Console.WriteLine(userNameLowercase);
            }
            catch (UserNotFoundException ex)
            {
                Console.WriteLine($"User {ex.MissingUserId} not found");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error occured: " + e.Message + Environment.NewLine + e.StackTrace);
            }
        }
    }
}