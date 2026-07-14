(() => {
  const STORAGE_KEY = "product-catalog.books.v1";
  const SETTINGS_KEY = "product-catalog.settings.v1";

  const seedBooks = [
    { id: 1, title: "Clean Code", author: "Robert C. Martin", description: "A handbook of agile software craftsmanship" },
    { id: 2, title: "Domain-Driven Design", author: "Eric Evans", description: "Tackling complexity in the heart of software" },
    { id: 3, title: "Designing Data-Intensive Applications", author: "Martin Kleppmann", description: "Reliability, scalability, and maintainability" },
    { id: 4, title: "The Pragmatic Programmer", author: "Andrew Hunt", description: "Your journey to mastery" },
    { id: 5, title: "Refactoring", author: "Martin Fowler", description: "Improving the design of existing code" },
    { id: 6, title: "Building Microservices", author: "Sam Newman", description: "Designing fine-grained systems" }
  ];

  const el = {
    catalog: document.getElementById("catalog"),
    statusText: document.getElementById("statusText"),
    apiHint: document.getElementById("apiHint"),
    apiBaseLabel: document.getElementById("apiBaseLabel"),
    search: document.getElementById("search"),
    authorFilter: document.getElementById("authorFilter"),
    sortBy: document.getElementById("sortBy"),
    sortDir: document.getElementById("sortDir"),
    pageSize: document.getElementById("pageSize"),
    prevPage: document.getElementById("prevPage"),
    nextPage: document.getElementById("nextPage"),
    pageInfo: document.getElementById("pageInfo"),
    dataMode: document.getElementById("dataMode"),
    openCreate: document.getElementById("openCreate"),
    bookDialog: document.getElementById("bookDialog"),
    bookForm: document.getElementById("bookForm"),
    dialogTitle: document.getElementById("dialogTitle"),
    bookId: document.getElementById("bookId"),
    title: document.getElementById("title"),
    author: document.getElementById("author"),
    description: document.getElementById("description"),
    closeDialog: document.getElementById("closeDialog"),
    cancelDialog: document.getElementById("cancelDialog"),
    apiDialog: document.getElementById("apiDialog"),
    apiForm: document.getElementById("apiForm"),
    apiBase: document.getElementById("apiBase"),
    closeApiDialog: document.getElementById("closeApiDialog"),
    toast: document.getElementById("toast")
  };

  const state = {
    page: 1,
    totalCount: 0,
    mode: "demo",
    apiBase: "http://localhost:8080",
    editingId: null
  };

  function loadSettings() {
    try {
      const raw = localStorage.getItem(SETTINGS_KEY);
      if (!raw) return;
      const parsed = JSON.parse(raw);
      state.mode = parsed.mode === "api" ? "api" : "demo";
      state.apiBase = parsed.apiBase || state.apiBase;
    } catch {
      /* ignore */
    }
  }

  function saveSettings() {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify({
      mode: state.mode,
      apiBase: state.apiBase
    }));
  }

  function loadDemoBooks() {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(seedBooks));
      return [...seedBooks];
    }
    try {
      return JSON.parse(raw);
    } catch {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(seedBooks));
      return [...seedBooks];
    }
  }

  function saveDemoBooks(books) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(books));
  }

  function toast(message) {
    el.toast.hidden = false;
    el.toast.textContent = message;
    clearTimeout(toast._t);
    toast._t = setTimeout(() => {
      el.toast.hidden = true;
    }, 2400);
  }

  function queryParams() {
    return {
      page: state.page,
      pageSize: Number(el.pageSize.value),
      search: el.search.value.trim(),
      author: el.authorFilter.value.trim(),
      sortBy: el.sortBy.value,
      sortDir: el.sortDir.value
    };
  }

  function compare(a, b, sortBy, sortDir) {
    const dir = sortDir === "desc" ? -1 : 1;
    const left = (a[sortBy] ?? "").toString().toLowerCase();
    const right = (b[sortBy] ?? "").toString().toLowerCase();
    if (sortBy === "id") return (Number(a.id) - Number(b.id)) * dir;
    return left.localeCompare(right) * dir;
  }

  async function listBooks() {
    const q = queryParams();
    if (state.mode === "demo") {
      let books = loadDemoBooks();
      if (q.search) {
        const s = q.search.toLowerCase();
        books = books.filter((b) =>
          [b.title, b.author, b.description].some((v) => (v || "").toLowerCase().includes(s))
        );
      }
      if (q.author) {
        const a = q.author.toLowerCase();
        books = books.filter((b) => (b.author || "").toLowerCase().includes(a));
      }
      books.sort((x, y) => compare(x, y, q.sortBy, q.sortDir));
      state.totalCount = books.length;
      const start = (q.page - 1) * q.pageSize;
      return books.slice(start, start + q.pageSize);
    }

    const url = new URL(`${state.apiBase.replace(/\/$/, "")}/query/api/v1/book`);
    Object.entries(q).forEach(([k, v]) => {
      if (v !== "" && v != null) url.searchParams.set(k, v);
    });
    const res = await fetch(url);
    if (!res.ok) throw new Error(`Query failed (${res.status})`);
    const data = await res.json();
    state.totalCount = data.totalCount ?? (data.items || []).length;
    return data.items || [];
  }

  async function createBook(payload) {
    if (state.mode === "demo") {
      const books = loadDemoBooks();
      const id = books.reduce((m, b) => Math.max(m, Number(b.id) || 0), 0) + 1;
      books.push({ id, ...payload });
      saveDemoBooks(books);
      return;
    }
    const res = await fetch(`${state.apiBase.replace(/\/$/, "")}/command/api/v1/book`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(`Create failed (${res.status})`);
  }

  async function updateBook(id, payload) {
    if (state.mode === "demo") {
      const books = loadDemoBooks();
      const idx = books.findIndex((b) => Number(b.id) === Number(id));
      if (idx < 0) throw new Error("Book not found");
      books[idx] = { ...books[idx], ...payload };
      saveDemoBooks(books);
      return;
    }
    const res = await fetch(`${state.apiBase.replace(/\/$/, "")}/command/api/v1/book/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error(`Update failed (${res.status})`);
  }

  async function deleteBook(id) {
    if (state.mode === "demo") {
      const books = loadDemoBooks().filter((b) => Number(b.id) !== Number(id));
      saveDemoBooks(books);
      return;
    }
    const res = await fetch(`${state.apiBase.replace(/\/$/, "")}/command/api/v1/book/${id}`, {
      method: "DELETE"
    });
    if (!res.ok) throw new Error(`Delete failed (${res.status})`);
  }

  function renderBooks(books) {
    if (!books.length) {
      el.catalog.innerHTML = `<p class="empty">No books match these filters. Try clearing search or add a new title.</p>`;
      return;
    }

    el.catalog.innerHTML = books.map((book) => `
      <article class="book-row" data-id="${book.id}">
        <div>
          <h2 class="book-title">${escapeHtml(book.title)}</h2>
          <p class="book-meta">#${book.id}${book.author ? ` · ${escapeHtml(book.author)}` : ""}</p>
          ${book.description ? `<p class="book-desc">${escapeHtml(book.description)}</p>` : ""}
        </div>
        <div class="row-actions">
          <button type="button" class="btn btn-ghost" data-action="edit">Edit</button>
          <button type="button" class="btn btn-danger" data-action="delete">Delete</button>
        </div>
      </article>
    `).join("");
  }

  function escapeHtml(value) {
    return String(value ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;");
  }

  function updatePager() {
    const pageSize = Number(el.pageSize.value);
    const totalPages = Math.max(1, Math.ceil(state.totalCount / pageSize) || 1);
    if (state.page > totalPages) state.page = totalPages;
    el.pageInfo.textContent = `Page ${state.page} of ${totalPages} · ${state.totalCount} books`;
    el.prevPage.disabled = state.page <= 1;
    el.nextPage.disabled = state.page >= totalPages;
  }

  async function refresh() {
    el.statusText.textContent = "Loading catalog…";
    try {
      const books = await listBooks();
      renderBooks(books);
      updatePager();
      el.statusText.textContent = state.mode === "demo"
        ? "Demo mode — data stored in this browser"
        : "Live API mode — command/query via gateway";
      el.apiHint.classList.toggle("hidden", state.mode !== "api");
      el.apiBaseLabel.textContent = state.apiBase;
    } catch (err) {
      el.catalog.innerHTML = `<p class="empty">${escapeHtml(err.message)}. Is the gateway running?</p>`;
      el.statusText.textContent = "Failed to load catalog";
      toast(err.message);
    }
  }

  function openCreateDialog() {
    state.editingId = null;
    el.dialogTitle.textContent = "Add book";
    el.bookId.value = "";
    el.bookForm.reset();
    el.bookDialog.showModal();
    el.title.focus();
  }

  function openEditDialog(book) {
    state.editingId = book.id;
    el.dialogTitle.textContent = "Edit book";
    el.bookId.value = book.id;
    el.title.value = book.title || "";
    el.author.value = book.author || "";
    el.description.value = book.description || "";
    el.bookDialog.showModal();
    el.title.focus();
  }

  function debounce(fn, ms) {
    let t;
    return (...args) => {
      clearTimeout(t);
      t = setTimeout(() => fn(...args), ms);
    };
  }

  const refreshDebounced = debounce(() => {
    state.page = 1;
    refresh();
  }, 220);

  el.catalog.addEventListener("click", async (event) => {
    const button = event.target.closest("button[data-action]");
    if (!button) return;
    const row = button.closest(".book-row");
    const id = Number(row.dataset.id);
    const action = button.dataset.action;

    if (action === "edit") {
      const title = row.querySelector(".book-title").textContent;
      const meta = row.querySelector(".book-meta").textContent;
      const desc = row.querySelector(".book-desc")?.textContent || "";
      const authorMatch = meta.split("·")[1]?.trim() || "";
      openEditDialog({ id, title, author: authorMatch, description: desc });
      return;
    }

    if (action === "delete") {
      if (!confirm(`Delete book #${id}?`)) return;
      try {
        await deleteBook(id);
        toast("Book deleted");
        await refresh();
      } catch (err) {
        toast(err.message);
      }
    }
  });

  el.bookForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const payload = {
      title: el.title.value.trim(),
      author: el.author.value.trim() || null,
      description: el.description.value.trim() || null
    };
    if (!payload.title) {
      toast("Title is required");
      return;
    }
    try {
      if (state.editingId == null) {
        await createBook(payload);
        toast("Book created");
      } else {
        await updateBook(state.editingId, payload);
        toast("Book updated");
      }
      el.bookDialog.close();
      await refresh();
    } catch (err) {
      toast(err.message);
    }
  });

  el.openCreate.addEventListener("click", openCreateDialog);
  el.closeDialog.addEventListener("click", () => el.bookDialog.close());
  el.cancelDialog.addEventListener("click", () => el.bookDialog.close());
  el.closeApiDialog.addEventListener("click", () => el.apiDialog.close());

  el.prevPage.addEventListener("click", () => {
    if (state.page > 1) {
      state.page -= 1;
      refresh();
    }
  });
  el.nextPage.addEventListener("click", () => {
    state.page += 1;
    refresh();
  });

  ["search", "authorFilter"].forEach((id) => {
    el[id].addEventListener("input", refreshDebounced);
  });
  ["sortBy", "sortDir", "pageSize"].forEach((id) => {
    el[id].addEventListener("change", () => {
      state.page = 1;
      refresh();
    });
  });

  el.dataMode.addEventListener("change", () => {
    if (el.dataMode.value === "api") {
      el.apiBase.value = state.apiBase;
      el.apiDialog.showModal();
      return;
    }
    state.mode = "demo";
    saveSettings();
    state.page = 1;
    refresh();
  });

  el.apiForm.addEventListener("submit", (event) => {
    event.preventDefault();
    state.apiBase = el.apiBase.value.trim().replace(/\/$/, "");
    state.mode = "api";
    el.dataMode.value = "api";
    saveSettings();
    el.apiDialog.close();
    state.page = 1;
    refresh();
  });

  el.apiDialog.addEventListener("close", () => {
    if (state.mode !== "api") {
      el.dataMode.value = "demo";
    }
  });

  loadSettings();
  el.dataMode.value = state.mode;
  el.apiBase.value = state.apiBase;
  refresh();
})();
