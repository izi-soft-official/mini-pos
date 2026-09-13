from fastapi import FastAPI

app = FastAPI(title="mini-pos AI service")


@app.get("/health")
def health():
    return {"status": "ok"}
