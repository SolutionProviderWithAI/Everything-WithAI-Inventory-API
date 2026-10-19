import logging
from contextlib import asynccontextmanager
from fastapi import FastAPI, HTTPException, status
from fastapi.middleware.cors import CORSMiddleware
from app.config import settings
from app.models import ChatMessageRequest, ChatMessageResponse, SessionResetRequest
from app.agent import process_chat_message, reset_session
import httpx

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(name)s: %(message)s")
logger = logging.getLogger("ChatbotApi")

@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("AI Chatbot Service initialized.")
    logger.info(f"Connecting to .NET Backend at: {settings.backend_api_url}")
    yield
    logger.info("AI Chatbot Service shutting down.")

app = FastAPI(
    title="AI Order & Inventory Management Chatbot Service",
    description="Python FastAPI service handling natural language understanding, stock checks, and order calculations.",
    version="1.0.0",
    lifespan=lifespan
)

# CORS setup
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/health", status_code=status.HTTP_200_OK)
async def health_check():
    backend_status = "unknown"
    try:
        async with httpx.AsyncClient(timeout=3.0) as client:
            resp = await client.get(f"{settings.backend_api_url.replace('/api', '')}/health")
            backend_status = "connected" if resp.status_code == 200 else f"status_{resp.status_code}"
    except Exception as e:
        backend_status = f"unreachable ({type(e).__name__})"

    return {
        "status": "Healthy",
        "service": "Python AI Chatbot Service",
        "backend_api": settings.backend_api_url,
        "backend_connection": backend_status
    }

@app.post("/chat/message", response_model=ChatMessageResponse)
async def chat_message(request: ChatMessageRequest):
    try:
        response = await process_chat_message(request)
        return response
    except Exception as e:
        logger.error(f"Error handling chat message: {e}", exc_info=True)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"An error occurred while processing your message: {str(e)}"
        )

@app.get("/chat/history/{session_id}")
async def get_history(session_id: str):
    """Retrieve chat conversation history from .NET backend."""
    url = f"{settings.backend_api_url}/chatbot/conversations/{session_id}/messages"
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            resp = await client.get(url)
            if resp.status_code == 200:
                return resp.json()
            return []
    except Exception as e:
        logger.warning(f"Could not fetch chat history: {e}")
        return []

@app.post("/chat/session/reset")
async def reset_chat_session(request: SessionResetRequest):
    reset_session(request.session_id)
    return {"message": f"Session '{request.session_id}' has been reset."}

if __name__ == "__main__":
    import uvicorn
    uvicorn.run("main:app", host=settings.host, port=settings.port, reload=True)
