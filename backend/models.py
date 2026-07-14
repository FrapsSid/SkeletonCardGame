from sqlalchemy import Column, Integer, String, ForeignKey
from sqlalchemy.orm import relationship
from database import Base


class Player(Base):
    __tablename__ = "players"

    id = Column(Integer, primary_key=True, index=True)
    nickname = Column(String)  # Отображаемое имя в игре

    # Новые поля для классической авторизации
    username = Column(String, unique=True, index=True, nullable=True)  # Логин
    hashed_password = Column(String, nullable=True)  # Зашифрованный пароль

    auths = relationship("PlayerAuth", back_populates="player")


class PlayerAuth(Base):
    __tablename__ = "player_auths"

    id = Column(Integer, primary_key=True, index=True)
    player_id = Column(Integer, ForeignKey("players.id"))
    provider = Column(String, index=True)  # "discord", "google" и т.д.
    provider_user_id = Column(String, index=True)

    player = relationship("Player", back_populates="auths")