using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class Email_OTP_Module
{
    private static readonly string DOMAIN = "@dso.org.sg";
    private const int OTP_LENGTH = 6;
    private const int OTP_VALIDITY_DURATION = 60000; // 1 minute in milliseconds
    private const int MAX_OTP_ATTEMPTS = 10;

    private string currentOTP;
    private DateTime otpGeneratedTime;
    private int otpAttempts;

    public void start()
    {
        // Initialize variables if necessary
    }

    public void close()
    {
        // Clean up resources if necessary
    }

    public string GenerateOTP()
    {
        Random random = new Random();
        string otp = random.Next(0, 999999).ToString("D6");
        return otp;
    }

    public bool ValidateEmailDomain(string email)
    {
        return email.EndsWith(DOMAIN, StringComparison.OrdinalIgnoreCase);
    }

    public bool SendEmail(string emailAddress, string emailBody)
    {
        try
        {
            // Assuming send_email is defined elsewhere
            send_email(emailAddress, emailBody);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public int generate_OTP_email(string user_email)
    {
        if (!Regex.IsMatch(user_email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            return STATUS_EMAIL_INVALID;
        }

        if (!ValidateEmailDomain(user_email))
        {
            return STATUS_EMAIL_INVALID;
        }

        currentOTP = GenerateOTP();
        otpGeneratedTime = DateTime.Now;
        otpAttempts = 0;

        string emailBody = $"Your OTP Code is {currentOTP}. The code is valid for 1 minute";

        if (SendEmail(user_email, emailBody))
        {
            return STATUS_EMAIL_OK;
        }
        else
        {
            return STATUS_EMAIL_FAIL;
        }
    }

    public int check_OTP(TextReader input)
    {
        var cts = new CancellationTokenSource(OTP_VALIDITY_DURATION);
        var token = cts.Token;

        while (otpAttempts < MAX_OTP_ATTEMPTS && DateTime.Now - otpGeneratedTime < TimeSpan.FromMilliseconds(OTP_VALIDITY_DURATION))
        {
            try
            {
                string userInput = Task.Run(() => input.ReadLine(), token).Result;
                if (string.IsNullOrEmpty(userInput))
                {
                    continue;
                }

                if (userInput == currentOTP)
                {
                    return STATUS_OTP_OK;
                }

                otpAttempts++;
            }
            catch (OperationCanceledException)
            {
                return STATUS_OTP_TIMEOUT;
            }
        }

        if (otpAttempts >= MAX_OTP_ATTEMPTS)
        {
            return STATUS_OTP_FAIL;
        }

        return STATUS_OTP_TIMEOUT;
    }

    private void send_email(string emailAddress, string emailBody)
    {
        // Implementation for sending email
        // Assuming this function is defined elsewhere or can use System.Net.Mail
        using (var client = new SmtpClient("smtp.yourserver.com"))
        {
            var mail = new MailMessage("your-email@dso.org.sg", emailAddress)
            {
                Subject = "Your OTP Code",
                Body = emailBody
            };
            client.Send(mail);
        }
    }

    public const int STATUS_EMAIL_OK = 0;
    public const int STATUS_EMAIL_FAIL = 1;
    public const int STATUS_EMAIL_INVALID = 2;
    public const int STATUS_OTP_OK = 3;
    public const int STATUS_OTP_FAIL = 4;
    public const int STATUS_OTP_TIMEOUT = 5;
}

namespace OTPModuleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Email_OTP_Module otpModule = new Email_OTP_Module();

            otpModule.start();

            Console.WriteLine("Enter your email address (only @dso.org.sg domain is allowed):");
            string userEmail = Console.ReadLine();

            int emailStatus = otpModule.generate_OTP_email(userEmail);
            if (emailStatus == Email_OTP_Module.STATUS_EMAIL_OK)
            {
                Console.WriteLine("OTP has been sent to your email. Please check your email and enter the OTP below:");
            }
            else if (emailStatus == Email_OTP_Module.STATUS_EMAIL_INVALID)
            {
                Console.WriteLine("The email address is invalid.");
                otpModule.close();
                return;
            }
            else if (emailStatus == Email_OTP_Module.STATUS_EMAIL_FAIL)
            {
                Console.WriteLine("Failed to send OTP to the email address.");
                otpModule.close();
                return;
            }

            using (StringReader reader = new StringReader(Console.ReadLine()))
            {
                int otpStatus = otpModule.check_OTP(reader);
                if (otpStatus == Email_OTP_Module.STATUS_OTP_OK)
                {
                    Console.WriteLine("OTP is valid and checked.");
                }
                else if (otpStatus == Email_OTP_Module.STATUS_OTP_FAIL)
                {
                    Console.WriteLine("Failed to enter valid OTP after 10 tries.");
                }
                else if (otpStatus == Email_OTP_Module.STATUS_OTP_TIMEOUT)
                {
                    Console.WriteLine("OTP entry timed out after 1 minute.");
                }
            }
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();

            otpModule.close();
        }
    }
}
