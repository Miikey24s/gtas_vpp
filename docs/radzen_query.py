#!/usr/bin/env python3
"""
Radzen Blazor MCP Query Tool
Wrapper for AI assistants to query Radzen Blazor documentation via MCP protocol.

Usage from AI tools:
    python3 radzen_query.py "How to create a DataGrid with CRUD"
    python3 radzen_query.py "RadzenDialog examples"
"""
import sys
import subprocess
import json

def query_radzen(question):
    """Query Radzen Blazor MCP and return formatted answer"""
    try:
        result = subprocess.run(
            ["python3", "radzen_mcp_client.py", "search", question],
            capture_output=True,
            text=True,
            timeout=30
        )
        
        if result.returncode != 0:
            return f"Error: {result.stderr}"
        
        data = json.loads(result.stdout)
        
        if "result" in data and "content" in data["result"]:
            content = data["result"]["content"]
            
            # Format output for AI consumption
            output = []
            for item in content:
                if item.get("type") == "text":
                    output.append(item["text"])
            
            return "\n\n---\n\n".join(output)
        
        return "No results found."
        
    except subprocess.TimeoutExpired:
        return "Error: Query timed out after 30 seconds."
    except json.JSONDecodeError:
        return f"Error: Invalid response format.\n{result.stdout}"
    except Exception as e:
        return f"Error: {str(e)}"

def main():
    if len(sys.argv) < 2:
        print("Usage: python3 radzen_query.py <question>")
        print("\nExamples:")
        print('  python3 radzen_query.py "How to create a DataGrid with CRUD"')
        print('  python3 radzen_query.py "RadzenDialog examples"')
        print('  python3 radzen_query.py "Form validation with Radzen"')
        sys.exit(1)
    
    question = " ".join(sys.argv[1:])
    answer = query_radzen(question)
    print(answer)

if __name__ == "__main__":
    main()
