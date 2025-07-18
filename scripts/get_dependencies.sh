#!/bin/bash

# Find the solution file in the current directory
solution_file=$(find . -maxdepth 1 -name "*.sln" -print -quit)

if [ -z "$solution_file" ]; then
    echo "No .sln file found in the current directory."
    exit 1
fi

echo "Found solution: $solution_file"
echo ""

# Get a list of project files and iterate through them
dotnet sln "$solution_file" list | grep ".*\.csproj" | while read -r project_path; do
    echo "Project: $project_path"
    
    # Get the dependencies for the current project
    dependencies=$(dotnet list "$project_path" reference)
    
    if [ -n "$dependencies" ]; then
        echo "  Dependencies:"
        # Format and print each dependency
        while IFS= read -r line; do
            echo "    - $line"
        done <<< "$dependencies"
    else
        echo "  No project dependencies."
    fi
    echo ""
done