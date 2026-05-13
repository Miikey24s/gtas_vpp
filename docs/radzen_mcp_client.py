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

class MCPSession:
    """MCP session manager"""
    def __init__(self, api_key):
        self.api_key = api_key
        self.session_id = None
    
    def request(self, method, params=None):
        """Send JSON-RPC request with session management"""
        headers = {
            "Content-Type": "application/json",
            "Accept": "application/json, text/event-stream",
            "X-Radzen-Key": self.api_key
        }
        
        # Add session ID for non-initialize requests
        if self.session_id and method != "initialize":
            headers["Mcp-Session-Id"] = self.session_id
        
        payload = {
            "jsonrpc": "2.0",
            "id": 1,
            "method": method,
            "params": params or {}
        }
        
        response = requests.post(MCP_URL, json=payload, headers=headers)
        response.raise_for_status()
        
        # Capture session ID from initialize response
        if method == "initialize" and "Mcp-Session-Id" in response.headers:
            self.session_id = response.headers["Mcp-Session-Id"]
        
        # Handle SSE response
        if response.headers.get('Content-Type') == 'text/event-stream':
            for line in response.text.split('\n'):
                if line.startswith('data: '):
                    return json.loads(line[6:])
        
        return response.json()
    
    def initialize(self):
        """Initialize MCP session"""
        return self.request("initialize", {
            "protocolVersion": "2025-03-26",
            "capabilities": {},
            "clientInfo": {
                "name": "kiro-cli-radzen",
                "version": "1.0.0"
            }
        })
    
    def list_tools(self):
        """List available tools"""
        return self.request("tools/list", {})
    
    def call_tool(self, tool_name, arguments):
        """Call a specific tool"""
        return self.request("tools/call", {
            "name": tool_name,
            "arguments": arguments
        })

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
        session = MCPSession(api_key)
        
        if command == "init":
            result = session.initialize()
            print(json.dumps(result, indent=2))
        
        elif command == "tools":
            session.initialize()  # Must initialize first
            result = session.list_tools()
            print(json.dumps(result, indent=2))
        
        elif command == "search":
            session.initialize()  # Must initialize first
            query = sys.argv[query_arg_index] if len(sys.argv) > query_arg_index else "RadzenDataGrid"
            result = session.call_tool("search", {"query": query})
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
