/**
 * Mirrors the C# BlogPost entity.
 * Interface (not class) because JSON has no methods — just data.
 */
export interface BlogPost {
  id: number;
  title: string;
  content: string;
  author: string;
  isPublished: boolean;
  createdAt: string; // ISO date string from the API
}
