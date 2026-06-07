export const APP_CONFIG = {
  BASE_URL:
    process.env.NEXT_PUBLIC_BASE_URL_CLIENT ||
    process.env.BASE_URL_CLIENT ||
    'NEXT_PUBLIC_BASE_URL_CLIENT and BASE_URL_CLIENT are not defined',
} as const;
