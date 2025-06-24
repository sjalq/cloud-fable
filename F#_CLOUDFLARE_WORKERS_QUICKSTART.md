# F# Cloudflare Workers - Rapid Setup Guide

**Proven working solution for F# + Cloudflare Workers without runtime exceptions**

## 🎯 Quick Start (5 minutes)

### Prerequisites
```bash
# Install required tools
dotnet tool install --global fable
npm install -g wrangler
```

### 1. Project Setup
```bash
mkdir my-fsharp-worker && cd my-fsharp-worker

# Create F# project
cat > my-worker.fsproj << 'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <DefineConstants>FABLE_COMPILER</DefineConstants>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="src/Worker.fs" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Fable.Core" Version="4.0.0" />
    <PackageReference Include="Fable.Promise" Version="3.2.0" />
  </ItemGroup>
</Project>
EOF

# Create package.json
cat > package.json << 'EOF'
{
  "name": "my-fsharp-worker",
  "scripts": {
    "build": "fable . --outDir dist && cp dist/src/Worker.js dist/worker.js && npx rollup -c",
    "deploy": "wrangler deploy"
  },
  "devDependencies": {
    "@rollup/plugin-alias": "^5.1.1",
    "@rollup/plugin-node-resolve": "^15.3.0",
    "rollup": "^4.28.1",
    "wrangler": "^3.0.0"
  }
}
EOF

# Create rollup config
cat > rollup.config.js << 'EOF'
import { nodeResolve } from '@rollup/plugin-node-resolve';
import alias from '@rollup/plugin-alias';
import path from 'path';

export default {
  input: 'dist/worker.js',
  output: {
    file: 'dist/bundle.js',
    format: 'iife',
    name: 'CloudflareWorker'
  },
  plugins: [
    alias({
      entries: [
        { find: /^\.\.\/fable_modules\/(.*)$/, replacement: path.resolve('dist/fable_modules/$1') }
      ]
    }),
    nodeResolve({ preferBuiltins: false })
  ]
};
EOF

# Create wrangler config
cat > wrangler.toml << 'EOF'
name = "my-fsharp-worker"
main = "dist/bundle.js"
compatibility_date = "2024-01-01"
EOF

npm install
```

### 2. Create Worker Code
```bash
mkdir src
cat > src/Worker.fs << 'EOF'
module Worker

open Fable.Core
open Fable.Core.JsInterop
open System

// Manual JS interop - proven to work
[<Emit("addEventListener('fetch', $0)")>]
let addEventListener (handler: obj -> unit) : unit = jsNative

[<Emit("new Response($0, {status: $1})")>]
let newResponse (body: string) (status: int) : obj = jsNative

[<Emit("new Response($0, {status: $1, headers: $2})")>]
let newResponseWithHeaders (body: string) (status: int) (headers: obj) : obj = jsNative

[<Emit("(new URL($0)).pathname")>]
let getPath (url: string) : string = jsNative

// Simple routing
let handleRequest (request: obj) =
    let url = request?url |> unbox<string>
    let method = request?method |> unbox<string>
    let path = getPath url
    
    match method, path with
    | "GET", "/" ->
        newResponse "Hello from F# Cloudflare Worker!" 200
    | "GET", "/health" ->
        let timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
        let body = sprintf """{"status":"healthy","timestamp":"%s"}""" timestamp
        let headers = createObj [ "content-type" ==> "application/json" ]
        newResponseWithHeaders body 200 headers
    | "GET", "/api/hello" ->
        newResponse "F# + Cloudflare = Success!" 200
    | _ ->
        let body = """{"message":"Not Found"}"""
        let headers = createObj [ "content-type" ==> "application/json" ]
        newResponseWithHeaders body 404 headers

// Register the fetch event listener
addEventListener (fun event ->
    let request = event?request
    let response = handleRequest request
    event?respondWith(response) |> ignore
)
EOF
```

### 3. Build & Deploy
```bash
npm run build
npm run deploy
```

## 🔧 Architecture

### Build Pipeline
```
F# Source → Fable Compiler → JavaScript → Rollup Bundler → Single Bundle → Cloudflare
```

### Key Components
- **Fable CLI**: Direct F# to JS compilation
- **Manual Interop**: `[<Emit>]` attributes for JS APIs
- **Rollup**: Module bundling with alias resolution
- **Wrangler**: Cloudflare deployment

## 📝 Code Patterns

### Request Handling
```fsharp
// Extract request data
let url = request?url |> unbox<string>
let method = request?method |> unbox<string>
let path = getPath url

// Pattern match routing
match method, path with
| "GET", "/" -> (* handle root *)
| "POST", "/api/users" -> (* handle API *)
| _ -> (* 404 response *)
```

### Response Creation
```fsharp
// Simple response
newResponse "Hello World!" 200

// With headers
let headers = createObj [ "content-type" ==> "application/json" ]
newResponseWithHeaders """{"status":"ok"}""" 200 headers
```

### Async Operations (if needed)
```fsharp
// For async responses, return Promise<obj>
[<Emit("new Promise(resolve => resolve($0))")>]
let promiseOf (value: obj) : JS.Promise<obj> = jsNative

let asyncHandler request =
    // Your async logic
    promiseOf (newResponse "Async result" 200)
```

## ⚠️ What NOT to Use

### ❌ Avoid These Approaches
- **Fable.CloudFlareWorkers library** - causes compilation errors
- **Webpack + fable-loader** - resolution issues with project files
- **ES module format** - use IIFE for Cloudflare Workers

### ❌ Common Pitfalls
- Don't use async/await syntax directly - stick to manual Promise creation
- Don't rely on automatic module resolution - use explicit bundling
- Don't mix browser DOM APIs with Worker APIs

## 🚀 Adding Features

### Environment Variables
```toml
# wrangler.toml
[vars]
API_KEY = "your-key"
DEBUG = "true"
```

```fsharp
// Access in F#
let apiKey = globalThis?API_KEY |> unbox<string>
```

### POST Body Handling
```fsharp
[<Emit("$0.text()")>]
let getText (request: obj) : JS.Promise<string> = jsNative

// Use in async context
let handlePost request =
    promise {
        let! body = getText request
        return newResponse body 200
    }
```

### KV Storage (paid plans)
```fsharp
[<Emit("MY_KV.get($0)")>]
let kvGet (key: string) : JS.Promise<string option> = jsNative

[<Emit("MY_KV.put($0, $1)")>]
let kvPut (key: string) (value: string) : JS.Promise<unit> = jsNative
```

## 🧪 Testing

### Local Testing
```bash
# Test build
npm run build

# Deploy to staging  
wrangler deploy --env staging

# Tail logs
wrangler tail
```

### Endpoint Testing
```bash
# Your worker URL (replace 'subdomain')
curl https://my-fsharp-worker.subdomain.workers.dev/
curl https://my-fsharp-worker.subdomain.workers.dev/health
curl https://my-fsharp-worker.subdomain.workers.dev/api/hello
```

## 💡 Performance Tips

- Keep bundle size minimal - avoid unnecessary F# libraries
- Use pattern matching for routing - very efficient
- Cache static responses where possible
- Consider using WASM for CPU-intensive tasks

## 🔗 Resources

- [Cloudflare Workers Docs](https://developers.cloudflare.com/workers/)
- [Fable Documentation](https://fable.io/)
- [Wrangler CLI](https://developers.cloudflare.com/workers/wrangler/)

---

**🎉 Success Criteria**: Your worker should respond without Error 1101 and handle all routes correctly! 