FROM postgres:18.6-bookworm

RUN apt-get update \
    && apt-get install --no-install-recommends -y python3 awscli ca-certificates curl \
    && curl --fail --silent --show-error --proto '=https' --tlsv1.2 \
       https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem \
       -o /etc/ssl/certs/rds-global-bundle.pem \
    && test -s /etc/ssl/certs/rds-global-bundle.pem \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /initializer
COPY infra/scripts/initialize-databases.py ./initialize-databases.py
COPY shared/database/LanguageWise.Shared.Db/sql/ ./sql/shared/
COPY mini-games-service/database/sql/ ./sql/mini_games/
COPY quizzes-courses-service/database/LanguageWise.QuizzesCoursesService.Db/sql/ ./sql/quizzes_courses/
COPY chat-discussion-service/database/LanguageWise.ChatDiscussionService.Db/sql/ ./sql/chat_discussion/
COPY quests-achievements-notifications-service/database/sql/ ./sql/quests_achievements_notifications/

ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1
USER postgres
ENTRYPOINT ["python3", "/initializer/initialize-databases.py"]