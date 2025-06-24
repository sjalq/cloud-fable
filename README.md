# Cloud Fable - F# Functional Programming Project

An F# project using Fable compiler with strict functional programming guidelines.

## 🚀 Quick Start

```bash
npm install           # Install dependencies
npm run build        # Compile F# to JavaScript
npm start            # Build and serve on localhost:8080
npm run watch        # Watch mode compilation
```

## 📋 Functional Programming Rules

This project enforces **pure functional programming** principles:

### ✅ **Allowed Patterns:**
- Immutable data structures (`let`, `List`, `Array`, `Map`, `Set`)
- Pure functions without side effects
- Pattern matching with `match` expressions
- Option types instead of null (`Some`, `None`)
- Result types for error handling (`Ok`, `Error`)
- Function composition and piping (`|>`)

### ❌ **Prohibited Patterns:**
- **Mutable variables** (`let mutable`)
- **Exceptions** (`failwith`, `throw`, `try/catch`)
- **Imperative loops** (`for`/`while` - use `List.map`, `Array.fold` instead)
- **Null references** (use `Option` types)
- **Side effects** in pure functions

## 🏗️ Architecture

```
cloud-fable/
├── src/
│   └── App.fs              # Main F# application
├── public/
│   ├── index.html          # HTML entry point
│   └── bundle.js           # Compiled JavaScript (generated)
├── dist/                   # Fable compilation output
├── cloud-fable.fsproj      # F# project file
├── package.json            # npm configuration
├── webpack.config.js       # Webpack bundling
└── .fablerc.json          # Fable compiler config
```

## 🔧 Example Functional Patterns

### Safe Parsing (No Exceptions)
```fsharp
let safeParseInt (input: string) =
    match System.Int32.TryParse(input) with
    | (true, value) -> Some value
    | (false, _) -> None
```

### Error Handling with Result Types
```fsharp
let divide x y =
    if y = 0 then 
        Error "Division by zero"
    else 
        Ok (x / y)
```

### Immutable Data Transformations
```fsharp
let processNumbers numbers =
    numbers
    |> List.filter (fun x -> x > 0)
    |> List.map (fun x -> x * 2)
    |> List.sum
```

## 🎯 Technologies

- **F# 8.0** - Functional programming language
- **Fable 4.25** - F# to JavaScript compiler  
- **Webpack 5** - Module bundling
- **Browser DOM** - Web APIs binding

## 📖 Resources

- [F# Documentation](https://docs.microsoft.com/en-us/dotnet/fsharp/)
- [Fable Documentation](https://fable.io/)
- [Functional Programming Principles](https://fsharpforfunandprofit.com/)

---

**Built with ❤️ using pure functional programming principles** 