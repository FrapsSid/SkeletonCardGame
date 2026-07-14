import os
import base64
import json as json_lib
from fastapi import FastAPI, Depends, HTTPException, status
from fastapi.responses import HTMLResponse
from sqlalchemy.ext.asyncio import AsyncSession
from sqlalchemy.future import select
from database import get_db
from models import Player, PlayerAuth
import httpx
from fastapi.responses import RedirectResponse
from auth import create_access_token, get_current_user, get_password_hash, verify_password
from dotenv import load_dotenv
from pydantic import BaseModel

# Загружаем переменные из .env
load_dotenv()

app = FastAPI()


class UserRegister(BaseModel):
    username: str
    password: str
    nickname: str


class UserLogin(BaseModel):
    username: str
    password: str


async def get_or_create_oauth_player(db: AsyncSession, provider: str, provider_id: str, discord_username: str):
    """
    Безопасная функция для входа через OAuth2.
    Если этот Дискорд заходит впервые, мы гарантированно создаем НОВОГО игрока,
    чтобы исключить угон чужих локальных аккаунтов по совпадению никнейма.
    """
    # 1. Проверяем, заходил ли этот Дискорд-аккаунт раньше
    result = await db.execute(
        select(PlayerAuth).filter_by(provider=provider, provider_user_id=provider_id)
    )
    auth = result.scalars().first()
    if auth:
        return await db.get(Player, auth.player_id)

    # 2. Если Дискорд новый, создаем для него отдельного уникального игрока
    # Чтобы username не конфликтовал с локальными регистрациями, можно добавить префикс или использовать id
    unique_username = f"discord_{provider_id}"

    new_player = Player(
        nickname=discord_username,
        username=unique_username,  # Безопасный внутренний логин
        hashed_password=None  # Локального пароля у него нет
    )
    db.add(new_player)
    await db.flush()  # Получаем id нового игрока до коммита

    # Создаем запись привязки
    new_auth = PlayerAuth(player_id=new_player.id, provider=provider, provider_user_id=provider_id)
    db.add(new_auth)
    await db.commit()
    return new_player


# Данные Дискорда из переменных окружения
DISCORD_CLIENT_ID = os.getenv("DISCORD_CLIENT_ID")
DISCORD_CLIENT_SECRET = os.getenv("DISCORD_CLIENT_SECRET")
BACKEND_REDIRECT_URI = "http://10.93.27.48:8000/auth/callback"


@app.get("/auth/login")
async def login(redirect_uri: str = None):
    state = ""
    if redirect_uri:
        state = base64.urlsafe_b64encode(json_lib.dumps({"redirect_uri": redirect_uri}).encode()).decode()

    url = (
        f"https://discord.com/oauth2/authorize?client_id={DISCORD_CLIENT_ID}"
        f"&redirect_uri={BACKEND_REDIRECT_URI}&response_type=code&scope=identify"
    )
    if state:
        url += f"&state={state}"
    return RedirectResponse(url)


@app.get("/auth/callback")
async def callback(code: str, state: str = None, db: AsyncSession = Depends(get_db)):
    async with httpx.AsyncClient() as client:
        token_response = await client.post("https://discord.com/api/oauth2/token", data={
            "client_id": DISCORD_CLIENT_ID,
            "client_secret": DISCORD_CLIENT_SECRET,
            "grant_type": "authorization_code",
            "code": code,
            "redirect_uri": BACKEND_REDIRECT_URI
        })
        token_data = token_response.json()

        if "access_token" not in token_data:
            raise HTTPException(status_code=400, detail="Discord auth error")

        user_response = await client.get("https://discord.com/api/users/@me", headers={
            "Authorization": f"Bearer {token_data['access_token']}"
        })
        user_data = user_response.json()

    player = await get_or_create_oauth_player(db, "discord", user_data['id'], user_data['username'])
    token = create_access_token({"sub": str(player.id), "nickname": player.nickname})

    if state:
        try:
            redirect_data = json_lib.loads(base64.urlsafe_b64decode(state))
            redirect_uri = redirect_data.get("redirect_uri")
            if redirect_uri:
                sep = "&" if "?" in redirect_uri else "?"
                return RedirectResponse(f"{redirect_uri}{sep}token={token}&nickname={player.nickname}")
        except Exception:
            pass

    return {"access_token": token, "token_type": "bearer", "player": player.nickname}


# ==========================================================
# ФУНКЦИЯ РУЧНОЙ ПРИВЯЗКИ ДИСКОРДА В ЛИЧНОМ КАБИНЕТЕ (БЕЗОПАСНАЯ)
# ==========================================================
@app.post("/profile/link/discord")
async def link_discord_account(
        code: str,
        db: AsyncSession = Depends(get_db),
        current_user: dict = Depends(get_current_user)
):
    """
    Юзер заходит под своим логином/паролем, получает JWT,
    и отправляет POST запрос на этот эндпоинт, передавая 'code' от Дискорда.
    """
    player_id = int(current_user["sub"])  # id того, кто сейчас авторизован

    # Обмениваем код Дискорда на данные его профиля
    async with httpx.AsyncClient() as client:
        token_response = await client.post("https://discord.com/api/oauth2/token", data={
            "client_id": DISCORD_CLIENT_ID,
            "client_secret": DISCORD_CLIENT_SECRET,
            "grant_type": "authorization_code",
            "code": code,
            "redirect_uri": REDIRECT_URI
        })
        token_data = token_response.json()

        if "access_token" not in token_data:
            raise HTTPException(status_code=400, detail="Не удалось получить токен Дискорда")

        user_response = await client.get("https://discord.com/api/users/@me", headers={
            "Authorization": f"Bearer {token_data['access_token']}"
        })
        discord_data = user_response.json()

    # 1. Проверяем, не привязан ли этот Дискорд к КОМУ-ТО ДРУГОМУ
    result = await db.execute(
        select(PlayerAuth).filter_by(provider="discord", provider_user_id=discord_data["id"])
    )
    existing_link = result.scalars().first()
    if existing_link:
        raise HTTPException(
            status_code=400,
            detail="Этот аккаунт Discord уже привязан к другому игровому профилю"
        )

    # 2. Проверяем, нет ли уже у текущего авторизованного игрока привязанного Дискорда
    result = await db.execute(
        select(PlayerAuth).filter_by(player_id=player_id, provider="discord")
    )
    already_linked = result.scalars().first()
    if already_linked:
        raise HTTPException(status_code=400, detail="У вас уже привязан аккаунт Discord")

    # 3. Если всё ок — создаем связь. Теперь у этого player_id есть и логин/пароль, и Дискорд!
    new_auth = PlayerAuth(player_id=player_id, provider="discord", provider_user_id=discord_data["id"])
    db.add(new_auth)
    await db.commit()

    return {"message": f"Аккаунт Discord ({discord_data['username']}) успешно привязан к вашему профилю!"}


@app.get("/profile")
async def get_profile(user_data: dict = Depends(get_current_user)):
    return {"message": "Доступ разрешен", "user": user_data}


@app.post("/auth/register")
async def register_user(user: UserRegister, db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Player).filter_by(username=user.username))
    existing_user = result.scalars().first()

    if existing_user:
        raise HTTPException(status_code=400, detail="Имя пользователя уже занято")

    hashed_pw = get_password_hash(user.password)
    new_player = Player(
        username=user.username,
        hashed_password=hashed_pw,
        nickname=user.nickname
    )

    db.add(new_player)
    await db.commit()
    return {"message": "Успешная регистрация! Теперь вы можете войти."}


@app.post("/auth/login/local")
async def login_local(user: UserLogin, db: AsyncSession = Depends(get_db)):
    result = await db.execute(select(Player).filter_by(username=user.username))
    player = result.scalars().first()

    if not player or not player.hashed_password:
        raise HTTPException(status_code=401, detail="Неверный логин или пароль")

    if not verify_password(user.password, player.hashed_password):
        raise HTTPException(status_code=401, detail="Неверный логин или пароль")

    token = create_access_token({"sub": str(player.id), "nickname": player.nickname})
    return {"access_token": token, "token_type": "bearer", "player": player.nickname}