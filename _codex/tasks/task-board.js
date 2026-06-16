const STORAGE_KEY = "tarena-task-board-v1";

const DEFAULT_TASKS = [
  { id: "skill-animations", title: "Animacje skilli", done: false },
  { id: "attack-trails", title: "Trailsy na atakach", done: false },
  { id: "character-unification", title: "Ujednolicenie postaci", done: false },
  { id: "finish-ui", title: "UI dokonczyc", done: false },
  { id: "metagame", title: "Metagame", done: false },
];

const taskList = document.querySelector("#taskList");
const statusNode = document.querySelector("#status");
const addTaskForm = document.querySelector("#addTaskForm");
const newTaskTitle = document.querySelector("#newTaskTitle");
const resetTasks = document.querySelector("#resetTasks");

let tasks = loadTasks();

function loadTasks() {
  const saved = localStorage.getItem(STORAGE_KEY);
  if (!saved) {
    return [...DEFAULT_TASKS];
  }

  try {
    const parsed = JSON.parse(saved);
    if (Array.isArray(parsed)) {
      return parsed
        .filter((task) => task && typeof task.title === "string")
        .map((task) => ({
          id: String(task.id || crypto.randomUUID()),
          title: task.title,
          done: Boolean(task.done),
        }));
    }
  } catch {
    // Bad saved data should not block opening the board.
  }

  return [...DEFAULT_TASKS];
}

function saveTasks() {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(tasks));
}

function render() {
  taskList.innerHTML = "";

  for (const task of tasks) {
    const item = document.createElement("li");
    item.className = `task${task.done ? " done" : ""}`;

    const checkbox = document.createElement("input");
    checkbox.type = "checkbox";
    checkbox.checked = task.done;
    checkbox.setAttribute("aria-label", `Zmien status: ${task.title}`);
    checkbox.addEventListener("change", () => {
      task.done = checkbox.checked;
      saveTasks();
      render();
    });

    const title = document.createElement("div");
    title.className = "task-title";
    title.textContent = task.title;

    const state = document.createElement("div");
    state.className = "task-state";
    state.textContent = task.done ? "Done" : "Todo";

    item.append(checkbox, title, state);
    taskList.append(item);
  }

  const doneCount = tasks.filter((task) => task.done).length;
  statusNode.textContent = `${doneCount} / ${tasks.length} Done`;
}

addTaskForm.addEventListener("submit", (event) => {
  event.preventDefault();

  const title = newTaskTitle.value.trim();
  if (!title) {
    return;
  }

  tasks.push({
    id: crypto.randomUUID(),
    title,
    done: false,
  });

  newTaskTitle.value = "";
  saveTasks();
  render();
});

resetTasks.addEventListener("click", () => {
  tasks = [...DEFAULT_TASKS];
  saveTasks();
  render();
});

render();
