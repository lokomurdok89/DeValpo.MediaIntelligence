"use strict";

const video = document.querySelector("#video");
const list = document.querySelector("#matches");
const previous = document.querySelector("#previous");
const next = document.querySelector("#next");
const play = document.querySelector("#play-selection");
const scoreFormat = new Intl.NumberFormat("es", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
let matches = [];
let selected = -1;
let loaded = false;
let catalog = [];

function timecode(seconds) {
  const millis = Math.round(seconds * 1000);
  return `${String(Math.floor(millis / 60000)).padStart(2, "0")}:${String(Math.floor(millis / 1000) % 60).padStart(2, "0")}.${String(millis % 1000).padStart(3, "0")}`;
}

function showError(message) {
  const element = document.querySelector("#error");
  element.textContent = message;
  element.hidden = false;
}

function playable(index) {
  return loaded && index >= 0 && index < matches.length && matches[index].start < video.duration;
}

function updateControls() {
  previous.disabled = !playable(selected - 1);
  next.disabled = !playable(selected + 1);
  play.disabled = !playable(selected);
  for (const [index, row] of Array.from(list.querySelectorAll("button")).entries()) {
    row.disabled = !playable(index);
    row.setAttribute("aria-current", String(index === selected));
    if (loaded && matches[index].start >= video.duration) row.title = "El momento está fuera de la duración de este video.";
  }
}

function selectMatch(index) {
  if (!playable(index)) return;
  selected = index;
  const match = matches[index];
  video.pause();
  video.currentTime = match.start;
  document.querySelector("#selected-title").textContent = `Coincidencia ${index + 1} · ${timecode(match.start)}`;
  document.querySelector("#selected-detail").textContent = `Similitud ${scoreFormat.format(match.similarity)} / 100 · Pausado para revisar`;
  updateControls();
  list.querySelector('[aria-current="true"]').scrollIntoView({ block: "nearest", behavior: "smooth" });
}

function scoreName(match) {
  return match.kind ? "Confianza" : "Similitud";
}

function addMatch(match, index) {
  const li = document.createElement("li");
  const button = document.createElement("button");
  button.type = "button";
  button.className = "match-row";
  button.disabled = true;
  button.setAttribute("aria-label", `Coincidencia ${index + 1}, ${timecode(match.start)}, ${scoreName(match)} ${scoreFormat.format(match.similarity)} de 100`);
  const number = document.createElement("span");
  number.className = "number";
  number.textContent = String(index + 1).padStart(2, "0");
  const time = document.createElement("span");
  time.className = "time";
  time.textContent = timecode(match.start);
  const kind = document.createElement("span");
  kind.className = "kind";
  kind.textContent = `${match.kind === "number" ? "Número visible" : match.kind === "detection" ? "Cara detectada" : "Coincidencia facial"} · ${match.start === match.end ? "Puntual" : `Hasta ${timecode(match.end)}`}`;
  time.append(kind);
  const score = document.createElement("span");
  score.className = "score";
  score.textContent = scoreFormat.format(match.similarity);
  const arrow = document.createElement("span");
  arrow.className = "arrow";
  arrow.textContent = "↗";
  button.append(number, time, score, arrow);
  button.addEventListener("click", () => selectMatch(index));
  li.append(button);
  list.append(li);
}

video.addEventListener("loadedmetadata", () => {
  loaded = Number.isFinite(video.duration) && video.duration > 0;
  document.querySelector("#duration").textContent = loaded ? `${timecode(video.duration)} · Video local` : "Duración no disponible";
  updateControls();
});
video.addEventListener("timeupdate", () => {
  document.querySelector("#position").textContent = timecode(video.currentTime);
});
video.addEventListener("error", () => {
  loaded = false;
  updateControls();
  showError("No se pudo reproducir el video. Usa una copia MP4/H.264 compatible con tu navegador.");
});
previous.addEventListener("click", () => selectMatch(selected - 1));
next.addEventListener("click", () => selectMatch(selected + 1));
play.addEventListener("click", async () => {
  if (!playable(selected)) return;
  video.currentTime = matches[selected].start;
  try {
    await video.play();
    document.querySelector("#selected-detail").textContent = `Similitud ${scoreFormat.format(matches[selected].similarity)} / 100 · Reproduciendo`;
  } catch {
    showError("No se pudo iniciar la reproducción. Usa los controles del video.");
  }
});

function selectVideo(index) {
  const data = catalog[index];
  video.pause();
  loaded = false;
  selected = -1;
  matches = data.matches;
  list.replaceChildren();
  document.querySelector("#error").hidden = true;
  document.querySelector("#position").textContent = timecode(0);
  document.querySelector("#duration").textContent = "Cargando video…";
  document.querySelector("#selected-title").textContent = "Selecciona un momento";
  document.querySelector("#selected-detail").textContent = "El video se pausará en el hallazgo para revisarlo.";
  document.querySelector("#video-label").textContent = data.label;
  document.querySelector("#provider").textContent = data.provider || "SIN EXPORTACIÓN ASOCIADA";
  document.querySelector("#source-kind").textContent = data.availability === "summary" ? "Resumen guardado" : data.availability === "unavailable" ? "Sin resultado" : "JSON guardado";
  document.querySelector("#count").textContent = data.totalMatches ?? "—";
  const google = data.provider === "Google Video Intelligence";
  document.querySelector("#score-label").textContent = google ? "CONFIANZA / 100" : "SIMILITUD / 100";
  document.querySelector("#footnote").textContent = google
    ? "Números visibles y caras genéricas. Detectar una cara no identifica a una persona."
    : "Son ocurrencias, no personas distintas. La similitud no es una probabilidad de identidad.";
  let empty = "";
  if (data.availability === "unavailable") {
    document.querySelector("#summary").textContent = "Resultado no disponible para este video.";
    empty = "Puedes reproducirlo. No hay un JSON asociado: no significa que una búsqueda haya devuelto cero coincidencias.";
  } else {
    const issues = data.issuesCount === null ? (data.hasIssues ? "Con incidencias" : "Sin incidencias reportadas") : `${data.issuesCount} incidencias reportadas`;
    document.querySelector("#summary").textContent = `Estado guardado: ${data.status} · ${issues}.`;
    if (data.availability === "summary") {
      empty = `El resumen registra ${data.totalMatches} coincidencias, pero no contiene timestamps. No hay momentos para seleccionar.`;
    } else if (!matches.length) {
      empty = "Sin coincidencias en el resultado guardado.";
    }
    if (data.hasIssues || data.status !== "Completed") {
      showError("El resultado guardado tiene incidencias o no está completado; no representa una búsqueda completa sin hallazgos.");
    }
  }
  matches.forEach(addMatch);
  if (empty) {
    const li = document.createElement("li");
    li.className = "empty";
    li.textContent = empty;
    list.append(li);
  }
  document.querySelectorAll(".video-choice").forEach((button, i) => button.setAttribute("aria-pressed", String(i === index)));
  updateControls();
  video.src = `media/${data.id}`;
  video.load();
}

function renderGallery() {
  const gallery = document.querySelector("#gallery");
  catalog.forEach((data, index) => {
    const button = document.createElement("button");
    button.type = "button";
    button.className = "video-choice";
    button.setAttribute("aria-label", `Ver ${data.label}`);
    const preview = document.createElement("video");
    preview.muted = true;
    preview.playsInline = true;
    preview.preload = "metadata";
    preview.setAttribute("aria-hidden", "true");
    preview.src = `media/${data.id}`;
    preview.addEventListener("loadedmetadata", () => { preview.currentTime = Math.min(0.1, preview.duration / 2); }, { once: true });
    const name = document.createElement("strong");
    name.textContent = data.label;
    const detail = document.createElement("span");
    detail.textContent = data.availability === "unavailable" ? "Resultado no disponible"
      : `${data.totalMatches} hallazgos · ${data.availability === "summary" ? "Solo resumen" : data.provider}${data.hasIssues ? " · Con incidencias" : ""}`;
    button.append(preview, name, detail);
    button.addEventListener("click", () => selectVideo(index));
    gallery.append(button);
  });
}

async function load() {
  try {
    const response = await fetch("catalog", { cache: "no-store" });
    if (!response.ok) throw new Error("No se pudo cargar el catálogo.");
    catalog = (await response.json()).videos;
    renderGallery();
    const firstWithMatches = catalog.findIndex(item => item.matches.length > 0);
    selectVideo(firstWithMatches < 0 ? 0 : firstWithMatches);
  } catch {
    showError("No se pudo leer el catálogo. Reinicia el visor con los archivos guardados.");
    document.querySelector("#summary").textContent = "Resultado no disponible";
  }
}

load();
