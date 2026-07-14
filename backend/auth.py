import os
from datetime import datetime, timedelta, timezone
from jose import jwt
from fastapi import HTTPException, Security
from fastapi.security import HTTPBearer, HTTPAuthorizationCredentials
from dotenv import load_dotenv
from passlib.context import CryptContext

# Загружаем переменные из .env
load_dotenv()

SECRET_KEY = os.getenv("SECRET_KEY", "fallback_temporary_key")
ALGORITHM = "HS256"
security = HTTPBearer()

# Настройка контекста для хеширования паролей
pwd_context = CryptContext(schemes=["bcrypt"], deprecated="auto")

def get_password_hash(password: str):
    """Превращает обычный пароль в нечитаемый хеш"""
    return pwd_context.hash(password)

def verify_password(plain_password: str, hashed_password: str):
    """Сравнивает введенный пароль с хешем из базы"""
    return pwd_context.verify(plain_password, hashed_password)

def create_access_token(data: dict):
    """Генерирует JWT токен"""
    to_encode = data.copy()
    expire = datetime.now(timezone.utc) + timedelta(hours=24)
    to_encode.update({"exp": expire})
    return jwt.encode(to_encode, SECRET_KEY, algorithm=ALGORITHM)

def verify_token(token: str):
    """Расшифровывает JWT токен"""
    try:
        return jwt.decode(token, SECRET_KEY, algorithms=[ALGORITHM])
    except:
        return None

def get_current_user(credentials: HTTPAuthorizationCredentials = Security(security)):
    """Проверяет токен при запросах к защищенным эндпоинтам"""
    token = credentials.credentials
    payload = verify_token(token)
    if payload is None:
        raise HTTPException(status_code=401, detail="Неверный или просроченный токен")
    return payload