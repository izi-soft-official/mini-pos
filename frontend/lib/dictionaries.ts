import en from "./dictionaries/en.json";
import fr from "./dictionaries/fr.json";

export const dictionaries: Record<string, typeof en> = {
  en,
  fr,
};

// Fallback utility function safely fetching keys
export function getTranslation(lang: string) {
  return dictionaries[lang] || dictionaries.en;
}
