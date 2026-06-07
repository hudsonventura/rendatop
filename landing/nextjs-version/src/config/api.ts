export const API_CONFIG = {
  BASE_URL:
    process.env.NEXT_PUBLIC_BASE_URL_SERVER ||
    process.env.BASE_URL_SERVER ||
    'NEXT_PUBLIC_BASE_URL_SERVER and BASE_URL_SERVER are not defined',
} as const;
