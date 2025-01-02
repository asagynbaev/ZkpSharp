# ZkpSharp

**ZkpSharp** is a .NET library for implementing Zero-Knowledge Proofs (ZKP). This library allows you to securely prove certain information (such as age or balance) without revealing the actual data. It uses cryptographic hashes and salts to ensure privacy and security.

## Features

- **Proof of Age**: Prove that your age is above a certain threshold without revealing your actual birthdate.
- **Proof of Balance**: Prove that you have sufficient balance to make a transaction without revealing your full balance.
- **Secure Hashing**: Uses SHA-256 hashing combined with a random salt to ensure secure and non-reversible proofs.

## Installation

You can install **ZkpSharp** via NuGet. Run the following command in your project directory:

```bash
dotnet add package ZkpSharp

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