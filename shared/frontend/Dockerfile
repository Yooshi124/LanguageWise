# Frontend microservice for the shared (home page) service.
# Static HTML, CSS and JavaScript served by nginx — there is no .NET in this tier.
#
# NOTE: the build context is the repository root, so that the shared CSS theme in
#       shared/frontend/css/ can be copied into every frontend image.

FROM nginx:1.29-alpine

COPY shared/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY shared/frontend/index.html /usr/share/nginx/html/index.html
COPY shared/frontend/css/       /usr/share/nginx/html/css/
COPY shared/frontend/js/        /usr/share/nginx/html/js/

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=3s --start-period=5s --retries=5 \
    CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1
