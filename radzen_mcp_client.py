#!/usr/bin/env python3
"""Minimal MCP client for Radzen Blazor MCP server"""
import json
import sys
import os
import requests

MCP_URL = "https://app.radzen.com/mcp"

def load_api_key():
    """Load API key from .env file or environment variable"""
    # Try environment variable first
    api_key = os.getenv("RADZEN_API_KEY")
    if api_key:
        return api_key
    
    # Try .env file
    env_path = os.path.join(os.path.dirname(__file__), ".env")
    if os.path.exists(env_path):
        with open(env_path, "r") as f:
            for line in f:
                line = line.strip()
                if line.startswith("RADZEN_API_KEY="):
                    return line.split("=", 1)[1].strip().strip('"').strip("'")
    
    return None

def mcp_request(method, params=None, api_key=None):
    """Send JSON-RPC request to MCP server"""
    headers = {
        "Content-Type": "application/json",
        "Accept": "application/json, text/event-stream",
        "X-Radzen-Key": api_key or "YOUR-LICENSE-KEY"
    }
    
    payload = {
        "jsonrpc": "2.0",
        "id": 1,
        "method": method,
        "params": params or {}
    }
    
    response = requests.post(MCP_URL, json=payload, headers=headers)
    response.raise_for_status()
    
    # Handle SSE response
    if response.headers.get('Content-Type') == 'text/event-stream':
        for line in response.text.split('\n'):
            if line.startswith('data: '):
                return json.loads(line[6:])  # Remove 'data: ' prefix
    
    # Handle plain JSON
    return response.json()

def initialize(api_key):
    """Initialize MCP session"""
    return mcp_request("initialize", {
        "protocolVersion": "2025-03-26",
        "capabilities": {},
        "clientInfo": {
            "name": "kiro-cli-test",
            "version": "1.0.0"
        }
    }, api_key)

def list_tools(api_key):
    """List available tools"""
    return mcp_request("tools/list", {}, api_key)

def call_tool(tool_name, arguments, api_key):
    """Call a specific tool"""
    return mcp_request("tools/call", {
        "name": tool_name,
        "arguments": arguments
    }, api_key)

def main():
    # Load API key from .env or argument
    api_key = load_api_key()
    
    if len(sys.argv) > 1 and not sys.argv[1].startswith("-"):
        # If first arg looks like a key (long string), use it
        if len(sys.argv[1]) > 30:
            api_key = sys.argv[1]
            command = sys.argv[2] if len(sys.argv) > 2 else "init"
            query_arg_index = 3
        else:
            command = sys.argv[1]
            query_arg_index = 2
    else:
        command = "init"
        query_arg_index = 2
    
    if not api_key:
        print("Error: RADZEN_API_KEY not found!")
        print("\nOptions:")
        print("  1. Set in .env file: RADZEN_API_KEY=your_key_here")
        print("  2. Set environment variable: export RADZEN_API_KEY=your_key_here")
        print("  3. Pass as argument: python3 radzen_mcp_client.py YOUR_KEY command")
        sys.exit(1)
    
    try:
        if command == "init":
            result = initialize(api_key)
            print(json.dumps(result, indent=2))
        
        elif command == "tools":
            result = list_tools(api_key)
            print(json.dumps(result, indent=2))
        
        elif command == "search":
            query = sys.argv[query_arg_index] if len(sys.argv) > query_arg_index else "RadzenDataGrid"
            result = call_tool("search", {"query": query}, api_key)
            print(json.dumps(result, indent=2))
        
        else:
            print(f"Unknown command: {command}")
            print("\nUsage: python3 radzen_mcp_client.py [API_KEY] [command] [args]")
            print("\nCommands:")
            print("  init              - Initialize and show server capabilities")
            print("  tools             - List available tools")
            print("  search <query>    - Search Radzen Blazor docs")
            sys.exit(1)
            
    except requests.exceptions.HTTPError as e:
        print(f"HTTP Error: {e}")
        print(f"Response: {e.response.text}")
        sys.exit(1)
    except Exception as e:
        print(f"Error: {e}")
        sys.exit(1)

if __name__ == "__main__":
    main()
