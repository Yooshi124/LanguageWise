ARG APPLICATION_IMAGE
FROM alpine:3.22 AS certificates
RUN apk add --no-cache curl ca-certificates \
    && curl --fail --silent --show-error --proto '=https' --tlsv1.2 \
       https://truststore.pki.rds.amazonaws.com/global/global-bundle.pem \
       -o /rds-global-bundle.pem \
    && test -s /rds-global-bundle.pem

FROM ${APPLICATION_IMAGE}
COPY --from=certificates /rds-global-bundle.pem /etc/ssl/certs/rds-global-bundle.pem