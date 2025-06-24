module App

open Browser.Dom

// Pure functional approach - no mutables, no exceptions
let createMessage (name: string) =
    $"Hello, {name}! This is a pure functional F# app"

let safeParseInt (input: string) =
    match System.Int32.TryParse(input) with
    | (true, value) -> Some value
    | (false, _) -> None

// Pure function for DOM manipulation
let updateElement (elementId: string) (content: string) =
    match document.getElementById(elementId) with
    | null -> ()  // Handle gracefully without exceptions
    | element -> element.textContent <- content

// Application entry point
let main () =
    let message = createMessage "Functional Programmers"
    updateElement "app" message
    
    console.log("F# Fable app started with strict functional rules!")

// Start the application
main() 