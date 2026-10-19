import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';

const services = [
  'shared',
  'chat-discussion-service',
  'mini-games-service',
  'quizzes-courses-service',
  'quests-achievements-notifications-service',
  'leaderboard-analytics-service',
];

for (const service of services) {
  test(`${service} uses a filtered runtime nginx template`, () => {
    const root = new URL(`../../${service}/frontend/`, import.meta.url);
    const dockerfile = readFileSync(new URL('Dockerfile', root), 'utf8');
    const template = readFileSync(new URL('nginx.conf', root), 'utf8');
    assert.match(dockerfile, /COPY nginx\.conf \/etc\/nginx\/templates\/default\.conf\.template/);
    assert.match(dockerfile, /DNS_RESOLVER=127\.0\.0\.11 DNS_SUFFIX="" FORWARDED_SCHEME='\$scheme'/);
    assert.match(dockerfile, /NGINX_ENVSUBST_FILTER='\^\(DNS_RESOLVER\|DNS_SUFFIX\|FORWARDED_SCHEME\)\$'/);
    assert.match(template, /resolver \$\{DNS_RESOLVER\} valid=10s ipv6=off;/);
    for (const upstream of template.matchAll(/http:\/\/[^";\s]+/g)) {
      assert.match(upstream[0], /\$\{DNS_SUFFIX\}:\d+$/);
    }
    assert.match(template, /try_files \$uri/);
    if (service === 'shared') {
      const headers = [...template.matchAll(/proxy_set_header\s+X-Forwarded-Proto ([^;]+);/g)];
      assert.ok(headers.length > 0);
      for (const header of headers) {
        assert.equal(header[1], '${FORWARDED_SCHEME}');
      }
      assert.doesNotMatch(template, /\$http_x_forwarded_proto/);
    }
    for (const proxy of template.matchAll(/proxy_pass ([^;]+);/g)) {
      assert.match(proxy[1], /^\$/);
    }
  });
}