export const API_CONFIG = {
  BASE_URL:
    process.env.NEXT_PUBLIC_BASE_URL_SERVER ||
    process.env.BASE_URL_SERVER ||
    'http://localhost:5000',
} as const;
