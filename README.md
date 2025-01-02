# ZkpSharp

**ZkpSharp** is a .NET library for implementing Zero-Knowledge Proofs (ZKP). This library allows you to securely prove certain information (such as age or balance) without revealing the actual data. It uses cryptographic hashes and salts to ensure privacy and security.

## Features

- **Proof of Age**: Prove that your age is above a certain threshold without revealing your actual birthdate.
- **Proof of Balance**: Prove that you have sufficient balance to make a transaction without revealing your full balance.
- **Proof of Membership**: Prove that a given value belongs to a set of valid values (e.g., proving you belong to a specific group).
- **Proof of Range**: Prove that a value lies within a specified range without revealing the exact value.
- **Proof of Time Condition**: Prove that an event occurred before or after a specified date without revealing the event date.
- **Secure Hashing**: Uses SHA-256 hashing combined with a random salt to ensure secure and non-reversible proofs.


## Installation

You can install **ZkpSharp** via NuGet. Run the following command in your project directory:

```bash
dotnet add package ZkpSharp
```

## Setup

Before using the ZkpSharp library, you need to configure a secret key for HMAC (SHA-256) hashing. This key is required for generating and verifying proofs.

### Setting Up the HMAC Key in Code

Instead of using environment variables, you can pass the HMAC secret key directly when creating the ProofProvider. The key should be a 256-bit key (32 bytes) encoded in Base64.

Here’s an example of how to configure the HMAC key directly in your application:

```csharp
using ZkpSharp;
using ZkpSharp.Security;
using System;

class Program
{
    static void Main()
    {
        // Example base64-encoded HMAC secret key (256 bits / 32 bytes)
        string hmacSecretKeyBase64 = "your-base64-encoded-key-here";

        // Create an instance of ProofProvider with the provided HMAC key
        var proofProvider = new ProofProvider(hmacSecretKeyBase64);

        var zkp = new ZKP(proofProvider);
        var dateOfBirth = new DateTime(2000, 1, 1); // The user's date of birth

        // Generate proof of age
        var (proof, salt) = zkp.ProveAge(dateOfBirth);

        // Verify the proof of age
        bool isValid = zkp.VerifyAge(proof, dateOfBirth, salt);
        Console.WriteLine($"Age proof valid: {isValid}");
    }
}
```

## Usage

### Proof of Age

You can prove that you are over a certain age (e.g., 18 years old) without revealing your birthdate.

#### Example:

```csharp
using ZkpSharp;
using System;

class Program
{
    static void Main()
    {
        var zkp = new ZKP();
        var dateOfBirth = new DateTime(2000, 1, 1); // The user's date of birth

        // Generate proof of age
        var (proof, salt) = zkp.ProveAge(dateOfBirth);

        // Verify the proof of age
        bool isValid = zkp.VerifyAge(proof, dateOfBirth, salt);
        Console.WriteLine($"Age proof valid: {isValid}");
    }
}
```

### Proof of Balance

You can prove that you have enough balance to make a transaction without revealing your actual balance.

#### Example:

```csharp
using ZkpSharp;

class Program
{
    static void Main()
    {
        var zkp = new ZKP();
        double balance = 1000.0; // The user's balance
        double requestedAmount = 500.0; // The amount the user wants to prove they can pay

        // Generate proof of balance
        var (proof, salt) = zkp.ProveBalance(balance, requestedAmount);

        // Verify the proof of balance
        bool isValidBalance = zkp.VerifyBalance(proof, requestedAmount, salt, balance);
        Console.WriteLine($"Balance proof valid: {isValidBalance}");
    }
}
```

## Contributing

We welcome contributions! To contribute:

1. Fork the repository.  
2. Create a new branch for your changes (`git checkout -b feature/your-feature`).  
3. Commit your changes (`git commit -m 'Add new feature'`).  
4. Push to your branch (`git push origin feature/your-feature`).  
5. Create a pull request.

Please ensure that your code passes all tests and adheres to the code style of the project.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contact

For questions, issues, or suggestions, feel free to open an issue or contact Azimbek Sagynbaev at [sagynbaev6@gmail.com].