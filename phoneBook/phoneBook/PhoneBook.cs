using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;
using System.Data.SQLite;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ksiazkaZDanymi
{
    internal class PhoneBook
    {
        private string path;

        private string databaseName;

        private SQLiteConnection connection;

        private SQLiteCommand commandHolder;

        private List<Person> persons = new List<Person>();

        public PhoneBook(string databaseName)
        {
            this.databaseName = databaseName;

            CreateDatabaseConnection();

            ShowMenu();
        }

        void CreateDatabaseConnection()
        {
            try
            {
                path = Path.GetFullPath(Path.Combine("..", "..", "..", databaseName));

                if (!File.Exists(path))
                {
                    Console.WriteLine(path);
                    throw new FileNotFoundException("Database with provided name does not exist.");
                }

                connection = new SQLiteConnection($"data source={path}; version=3");
                connection.Open();

                commandHolder = connection.CreateCommand();
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void AddToList()
        {
            Console.Clear();

            string name = "";
            string surname = "";
            string phoneNumber = "";
            string mail = "";
            string dateOfBirth = "";

            StringLengthAttribute nameSurnameValidation = new StringLengthAttribute(50);
            nameSurnameValidation.MinimumLength = 1;

            string dateValidationPattern = "^(?:\\d{4}[-/]\\d{2}[-/]\\d{2})$";
            Regex dateValidation = new Regex(dateValidationPattern);

            while (true)
            {
                try
                {
                    Console.WriteLine("Enter new user name: ");
                    name = Console.ReadLine();
                    nameSurnameValidation.Validate(name, "name");

                    Console.WriteLine("Enter new user surname: ");
                    surname = Console.ReadLine();
                    nameSurnameValidation.Validate(surname, "name");

                    Console.WriteLine("Enter new user phone number (eg. 222-222-222): ");
                    phoneNumber = Console.ReadLine();
                    new PhoneAttribute().Validate(phoneNumber, "phoneNumber");
                    new StringLengthAttribute(12).Validate(phoneNumber, "phoneNumber");

                    Console.WriteLine("Enter new user mail: ");
                    mail = Console.ReadLine();
                    new EmailAddressAttribute().Validate(mail, "mail");

                    Console.WriteLine("Enter new user birth date (eg. 2012-02-12): ");
                    dateOfBirth = Console.ReadLine();
                    if (dateValidation.IsMatch(dateOfBirth) == false)
                    {
                        throw new ValidationException("Pole dateOfBirth musi być poprawnym formatem daty (YYYY-MM-DD lub YYYY/MM/DD).");
                    }

                    commandHolder.CommandText = $"INSERT INTO Persons (name, surname, phone_number, mail, date_of_birth) VALUES (@name, @surname, @phoneNumber, @mail, @dateOfBirth)";

                    commandHolder.Parameters.AddWithValue("@name", name);
                    commandHolder.Parameters.AddWithValue("@surname", surname);
                    commandHolder.Parameters.AddWithValue("@phoneNumber", phoneNumber);
                    commandHolder.Parameters.AddWithValue("@mail", mail);
                    commandHolder.Parameters.AddWithValue("@dateOfBirth", dateOfBirth);

                    var userAdd = commandHolder.ExecuteNonQuery();
                    break;
                }
                catch (ValidationException ex)
                {
                    Console.WriteLine("Some provided data are incorrect: " + ex.Message);
                    Console.WriteLine("Enter data again.");
                    Console.WriteLine("Press any button to continue.");
                    Console.ReadKey();
                    Console.Clear();
                    continue;
                }
                catch (SqlException ex)
                {
                    throw new Exception("Something went wrong while performing INSERT query on database. Error communicate: " + ex.Message);
                }
            }

            Console.WriteLine("User successfully added.");
            Console.WriteLine("Press any button to return.");
            Console.ReadKey();
            Console.Clear();
        }

        public void DisplayListMembers()
        {
            Console.Clear();

            commandHolder.CommandText = "SELECT * FROM Persons";

            var persons = commandHolder.ExecuteReader();

            while (persons.Read())
            {
                Console.WriteLine(
                    $"{persons[0]}. {{ \n \t" +
                    $"Name: {persons[1]} \n \t" +
                    $"Surname: {persons[2]} \n \t" +
                    $"Phone Number: {persons[3]} \n \t" +
                    $"Mail: {persons[4]} \n \t" +
                    $"Date of Birth: {persons[5]} \n" +
                    $"}}");
            }

            Console.WriteLine("Press any button to return.");
            Console.ReadKey();
            Console.Clear();
        }

        public void DeleteFromList()
        {
            Console.Clear();

            int id = 0;

            Console.WriteLine("Enter id of user which should be removed");
            id = System.Convert.ToInt32(Console.ReadLine());


            if (id < 0 || id > persons[persons.Count - 1].ID)
            {
                throw new IndexOutOfRangeException("Provided ID is out of the current list indexes");
            }
            else
            {
                persons.RemoveAll(element => element.ID == id);
            }

            File.WriteAllText(path, string.Empty);

            StreamWriter sr = new StreamWriter(path);

            foreach (Person p in persons)
            {
                sr.WriteLine($"{p.ID} {p.Name} {p.Surname} {p.PhoneNumber};");
            }

            sr.Close();

            Console.WriteLine("Press any button to return.");
            Console.ReadKey();
            Console.Clear();
        }

        public void ModifyListMember()
        {
            Console.Clear();

            int id = 0;

            Console.WriteLine("Enter id of user which should be modified");
            id = System.Convert.ToInt32(Console.ReadLine());

            if (id < 0 || id > persons[persons.Count - 1].ID)
            {
                throw new IndexOutOfRangeException("Provided ID is out of the current list indexes");
            }

            string newName = "";
            string newSurname = "";
            string newPhoneNumber = "";

            Console.WriteLine("Enter new user name: ");
            newName = Console.ReadLine();

            Console.WriteLine("Enter new user surname: ");
            newSurname = Console.ReadLine();

            Console.WriteLine("Enter new user phoneNumber (eg. 222-222-222)");
            newPhoneNumber = Console.ReadLine();


            Person personToModify = persons.FirstOrDefault(person => person.ID == id);
            personToModify.Name = newName;
            personToModify.Surname = newSurname;
            personToModify.PhoneNumber = newPhoneNumber;

            File.WriteAllText(path, string.Empty);

            StreamWriter sr = new StreamWriter(path);

            foreach (Person p in persons)
            {
                sr.WriteLine($"{p.ID} {p.Name} {p.Surname} {p.PhoneNumber};");
            }

            sr.Close();

            Console.WriteLine("Press any button to return.");
            Console.ReadKey();
            Console.Clear();
        }

        private void ShowMenu()
        {
            while (true)
            {
                Console.WriteLine("Wybierz operację:");
                Console.WriteLine("1. Delete user with certain ID");
                Console.WriteLine("2. Add new user");
                Console.WriteLine("3. Display all users");
                Console.WriteLine("4. Modify user with certain ID");
                Console.WriteLine("5. Clear console");
                Console.WriteLine("6. Terminate the program");

                char choice = Console.ReadKey().KeyChar;
                Console.WriteLine();

                switch (choice)
                {
                    case '1':
                        DeleteFromList();
                        break;
                    case '2':
                        AddToList();
                        break;
                    case '3':
                        DisplayListMembers();
                        break;
                    case '4':
                        ModifyListMember();
                        break;
                    case '5':
                        Console.Clear();
                        Console.WriteLine("Console cleared");
                        break;
                    case '6':
                        Console.WriteLine("End of the program.");
                        return;
                    default:
                        Console.WriteLine("Option you selected is not available.");
                        break;
                }
            }
        }
    }
}
