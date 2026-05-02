# Synapse ASP.NET Core Example

14 API endpoints wrapping [pyrx-synapse-dotnet](https://github.com/pyrx-tech/pyrx-synapse-dotnet) with [ASP.NET Core Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis).

## Setup

1. Ensure .NET 8.0+ SDK is installed
2. Copy `.env.example` to `.env` and fill in your credentials
3. Export environment variables: `export $(cat .env | xargs)`

## Run

```bash
dotnet run
# Server starts on http://localhost:4014
```

## Endpoints

### Core
| Method | Path | Description |
|--------|------|-------------|
| POST | /api/track | Track event |
| POST | /api/track/batch | Batch track events |
| POST | /api/identify | Identify contact |
| POST | /api/identify/batch | Batch identify contacts |
| POST | /api/send | Send email |

### Contacts
| Method | Path | Description |
|--------|------|-------------|
| GET | /api/contacts | List contacts |
| GET | /api/contacts/{id} | Get contact |
| PUT | /api/contacts/{externalId} | Update contact |
| DELETE | /api/contacts/{externalId} | Delete contact |

### Templates
| Method | Path | Description |
|--------|------|-------------|
| GET | /api/templates | List templates |
| POST | /api/templates | Create template |
| GET | /api/templates/{slug} | Get template |
| PUT | /api/templates/{slug} | Update template |
| DELETE | /api/templates/{slug} | Delete template |
| POST | /api/templates/{slug}/preview | Preview template |

## Test

```bash
export SYNAPSE_API_KEY=psk_live_...
export SYNAPSE_WORKSPACE_ID=ws_...
bash test.sh
```

- [Synapse Docs](https://synapse.pyrx.tech/developers)
- [.NET SDK](https://synapse.pyrx.tech/developers/sdks/dotnet)
