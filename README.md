# Stream Chat Unity SDK Converter
Tool that generates Stream Chat Dotnet SDK 

# The internal process
1. Download latest default branch (develop) from [Stream Chat Unity SDK](https://github.com/GetStream/stream-chat-unity) repository
2. Remove Unity Engine related code
3. Replace SDK libraries with the ones downloaded from [Stream Chat SDK Dotnet Dependencies Library](https://github.com/sierpinskid/stream-chat-sdk-dotnet-dependencies-library)

# Supported modes
You can set the mode with `LibsMode` field in the `UnitySdkUnityEngineStripper.Config`
- Console App
- .NET MAUI (to be supported in the future)

# How to use
1. Generate GH access token that can download public repositores
2. Save it to `private_gh_token.txt` in the project root directory (this file is added to `.gitignore` so it won't be commited to Git)
3. Run console app
4. Prompt will ask you to provide target path for the generated SDK
