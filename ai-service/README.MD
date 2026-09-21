# AI Service

This folder contains the Python FastAPI service used by the Mini POS AI Assistant.

The AI service uses the Groq API to process natural-language POS requests and return structured responses for the backend.

## Environment Variables

Before running the AI service, you must create a `.env` file inside the `ai-service` folder.

Create:

```text
ai-service/.env
```

Add the following variables:

```
GROQ_API_KEY=your_groq_api_key

(use this key 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)

AI_MODEL=your_groq_model

(for now i'm using openai/gpt-oss-120b)
```

## Running the AI Service

From the ai-service directory:

```
pip install -r requirements.txt
```

Then start the FastAPI service:

```
uvicorn main:app --reload --port 8000
```

The AI service will then be available at:

```
http://localhost:8000
```

Health Check

You can verify that the service is running with:

```
GET /health
```

The health endpoint also reports the configured AI model.
