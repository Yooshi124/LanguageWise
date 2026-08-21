# Frontend microservice for student 1 (Mini Games).
# Static HTML, CSS and JavaScript served by nginx - there is no .NET in this tier.
#
# NOTE: the build context is the repository root. The CSS theme and the vendored
#       HTMX bundle are pulled from shared/frontend/ so that every frontend in the
#       application looks and behaves consistently (specification section 2.3).

FROM nginx:1.29-alpine

COPY student-1/frontend/nginx.conf  /etc/nginx/conf.d/default.conf
COPY student-1/frontend/index.html  /usr/share/nginx/html/index.html
COPY shared/frontend/css/       /usr/share/nginx/html/css/
COPY shared/frontend/js/htmx.min.js /usr/share/nginx/html/js/htmx.min.js

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1
