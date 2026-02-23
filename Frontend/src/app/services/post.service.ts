import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BlogPost } from '../models/post.model';
import { environment } from '../../environments/environment';

/**
 * Service that talks to the .NET API — same pattern as a .NET service:
 * - Injected via DI (providedIn: 'root' = singleton, like AddSingleton)
 * - Encapsulates all HTTP logic — components never call HttpClient directly
 * - Returns Observables (reactive data streams)
 */
@Injectable({ providedIn: 'root' })
export class PostService {
  private apiUrl = `${environment.apiUrl}/api/posts`;

  constructor(private http: HttpClient) {}

  /** GET /api/posts — all published posts */
  getPosts(): Observable<BlogPost[]> {
    return this.http.get<BlogPost[]>(this.apiUrl);
  }

  /** GET /api/posts/:id — single post by ID */
  getPost(id: number): Observable<BlogPost> {
    return this.http.get<BlogPost>(`${this.apiUrl}/${id}`);
  }

  /** POST /api/posts — create a new post (requires auth) */
  createPost(post: Omit<BlogPost, 'id' | 'createdAt'>): Observable<BlogPost> {
    return this.http.post<BlogPost>(this.apiUrl, post);
  }

  /** PUT /api/posts/:id — full update (requires auth) */
  updatePost(id: number, post: Omit<BlogPost, 'id' | 'createdAt'>): Observable<BlogPost> {
    return this.http.put<BlogPost>(`${this.apiUrl}/${id}`, post);
  }

  /** DELETE /api/posts/:id — remove a post (requires auth) */
  deletePost(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
