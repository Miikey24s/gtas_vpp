import { defineConfig } from '@hey-api/openapi-ts'

const input = process.env.GTAS_OPENAPI_URL ?? './openapi/gtas-vpp.openapi.json'

export default defineConfig({
  input,
  output: 'src/api/generated',
  plugins: [
    '@hey-api/typescript',
    '@hey-api/sdk',
    '@hey-api/client-fetch',
    {
      name: '@tanstack/react-query',
      mutationOptions: true,
      queryOptions: true,
    },
  ],
})
