"""NOCTURNE — IFM: protótipo visual local para partidas personalizadas."""
from pathlib import Path
import json
import time
import tkinter as tk
from tkinter import messagebox
import numpy as np
from PIL import ImageGrab, ImageTk

HOME = Path(__file__).resolve().parent


def candidates(rgb, width, threshold):
    """Heurística de preenchimento vermelho + restante escuro; não identifica campeão."""
    a = np.asarray(rgb).astype(np.int16)
    red = (a[:, :, 0] > 115) & (a[:, :, 0] > a[:, :, 1] * 1.5) & (a[:, :, 0] > a[:, :, 2] * 1.35)
    dark = a.max(axis=2) < 105
    found = []
    for y in range(1, a.shape[0] - 3, 2):
        row = red[y:y + 3].sum(axis=0) >= 2
        edges = np.diff(np.r_[False, row, False].astype(int))
        for x, end in zip(np.flatnonzero(edges == 1), np.flatnonzero(edges == -1)):
            length = end - x
            if not 6 <= length <= width * threshold or x + width > a.shape[1]:
                continue
            # Exige parte vazia escura à direita e separação à esquerda.
            if x > 1 and red[y:y + 3, x - 2:x].mean() > .25:
                continue
            empty = dark[y:y + 3, end:x + width]
            if empty.size and empty.mean() >= .78:
                if not any(abs(x - p[0]) < width // 2 and abs(y - p[1]) < 15 for p in found):
                    found.append((int(x), int(y), length / width))
    return found


class App:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title('NOCTURNE — IFM')
        self.root.geometry('600x760')
        self.region = None
        self.map_region = None
        self.running = False
        self.last_seen = None
        self.previous = []
        self.last_alarm = 0
        self.preview = None
        self.width = tk.IntVar(value=105)
        self.limit = tk.IntVar(value=30)
        self.expansion = tk.DoubleVar(value=.012)
        self.sound = tk.BooleanVar(value=True)
        self.status = tk.StringVar(value='Calibre a área do jogo e a largura de uma barra de campeão.')
        self.jungle = tk.StringVar(value='Jungler: sem aparição registrada.')
        tk.Label(self.root, text='NOCTURNE — IFM', font=('Segoe UI', 19, 'bold')).pack(pady=8)
        tk.Label(self.root, text='Protótipo para partidas personalizadas • análise somente da tela').pack()
        for title, action in [('1. Selecionar área de combate', lambda: self.select('game')),
                              ('2. Calibrar barra INTEIRA de campeão', lambda: self.select('bar')),
                              ('3. Selecionar minimapa', lambda: self.select('map'))]:
            tk.Button(self.root, text=title, command=action).pack(fill='x', padx=20, pady=3)
        controls = tk.Frame(self.root)
        controls.pack(pady=6)
        tk.Label(controls, text='Barra inteira (pixels):').grid(row=0, column=0)
        tk.Spinbox(controls, from_=20, to=400, textvariable=self.width, width=6).grid(row=0, column=1)
        tk.Label(controls, text='Avisar abaixo de (%):').grid(row=1, column=0)
        tk.Spinbox(controls, from_=5, to=70, textvariable=self.limit, width=6).grid(row=1, column=1)
        tk.Label(controls, text='Expansão da incerteza / segundo:').grid(row=2, column=0)
        tk.Spinbox(controls, from_=.001, to=.1, increment=.001, textvariable=self.expansion, width=6).grid(row=2, column=1)
        tk.Checkbutton(self.root, text='Alerta sonoro (intervalo mínimo de 8 segundos)', variable=self.sound).pack()
        tk.Button(self.root, text='Iniciar / pausar', command=self.toggle).pack(pady=5)
        tk.Label(self.root, textvariable=self.status, wraplength=560, fg='#a03020').pack(pady=6)
        tk.Label(self.root, text='Minimapa: clique onde VOCÊ acabou de ver o jungler inimigo.').pack()
        self.canvas = tk.Canvas(self.root, width=270, height=270, bg='#20252a')
        self.canvas.pack(pady=5)
        self.canvas.bind('<Button-1>', self.mark)
        tk.Button(self.root, text='Limpar última aparição', command=self.clear).pack()
        tk.Label(self.root, textvariable=self.jungle, wraplength=560).pack(pady=5)
        tk.Label(self.root, text='Área circular = possibilidade ilustrativa, sem rota, obstáculos ou velocidade real.\n'
                 'Não identifica nomes, alcance do R, escudos ou segurança do gank.\n'
                 'Barras de tropas e interface podem gerar falsos alertas.', wraplength=570).pack(pady=5)
        try:
            cfg = json.loads((HOME / 'config.json').read_text(encoding='utf-8'))
            self.width.set(cfg['width'])
            self.limit.set(cfg['limit'])
        except (OSError, ValueError, KeyError):
            pass
        self.root.after(500, self.tick)
        self.root.mainloop()

    def select(self, kind):
        self.running = False
        self.root.withdraw()
        self.root.after(350, lambda: self.selector(kind))

    def selector(self, kind):
        try:
            shot = ImageGrab.grab().convert('RGB')
        except Exception as exc:
            self.root.deiconify()
            messagebox.showerror('NOCTURNE — IFM', str(exc))
            return
        top = tk.Toplevel(self.root)
        top.title('NOCTURNE — IFM — Calibração')
        top.geometry(f'{shot.width}x{shot.height}+0+0')
        top.overrideredirect(True)
        top.attributes('-topmost', True)
        canvas = tk.Canvas(top, highlightthickness=0)
        canvas.pack(fill='both', expand=True)
        photo = ImageTk.PhotoImage(shot)
        canvas.create_image(0, 0, image=photo, anchor='nw')
        canvas.image = photo
        canvas.create_text(20, 20, anchor='nw', text='Arraste um retângulo. Esc cancela.', fill='yellow', font=('Segoe UI', 18, 'bold'))
        state = {}
        def down(event):
            state['start'] = (event.x, event.y)
            if 'rect' in state:
                canvas.delete(state['rect'])
            state['rect'] = canvas.create_rectangle(event.x, event.y, event.x, event.y, outline='yellow', width=2)
        def move(event):
            if 'start' in state:
                canvas.coords(state['rect'], *state['start'], event.x, event.y)
        def finish(event):
            if 'start' not in state:
                return
            x, y = state['start']
            box = (min(x, event.x), min(y, event.y), max(x, event.x), max(y, event.y))
            top.destroy()
            self.root.deiconify()
            if box[2] - box[0] < 10 or box[3] - box[1] < 2:
                return
            if kind == 'bar':
                self.width.set(box[2] - box[0])
            elif kind == 'game':
                self.region = box
            else:
                self.map_region = box
            self.status.set('Calibração aplicada. Inicie quando estiver pronto.')
        canvas.bind('<ButtonPress-1>', down)
        canvas.bind('<B1-Motion>', move)
        canvas.bind('<ButtonRelease-1>', finish)
        top.bind('<Escape>', lambda e: (top.destroy(), self.root.deiconify()))
        top.focus_force()

    def toggle(self):
        if not self.region:
            messagebox.showinfo('NOCTURNE — IFM', 'Selecione primeiro a área de combate.')
            return
        try:
            if not 20 <= self.width.get() <= 400 or not 5 <= self.limit.get() <= 70 or not .001 <= self.expansion.get() <= .1:
                raise ValueError()
            (HOME / 'config.json').write_text(json.dumps({'width': self.width.get(), 'limit': self.limit.get()}), encoding='utf-8')
        except (ValueError, tk.TclError, OSError):
            messagebox.showerror('NOCTURNE — IFM', 'Confira os valores e a permissão para salvar a configuração.')
            return
        self.running = not self.running
        self.previous = []
        self.status.set('Monitorando barras visíveis…' if self.running else 'Pausado.')

    def clear(self):
        self.last_seen = None
        self.jungle.set('Jungler: sem aparição registrada.')

    def mark(self, event):
        if self.map_region:
            self.last_seen = (event.x / 270, event.y / 270, time.monotonic())

    def tick(self):
        try:
            now = time.monotonic()
            if self.running:
                shot = ImageGrab.grab(bbox=self.region).convert('RGB')
                hits = candidates(shot, self.width.get(), self.limit.get() / 100)
                stable = [p for p in hits if any(abs(p[0] - q[0]) < 45 and abs(p[1] - q[1]) < 30 for q in self.previous)]
                self.previous = hits
                if stable:
                    p = min(stable, key=lambda p: p[2])
                    self.status.set(f'Possível barra baixa: ~{p[2]:.0%} • posição na área: {p[0]}, {p[1]}. Confira na tela!')
                    if self.sound.get() and now - self.last_alarm > 8:
                        self.root.bell()
                        self.last_alarm = now
                else:
                    self.status.set('Monitorando • nenhuma barra baixa confirmada pela heurística.')
            if self.map_region:
                self.preview = ImageTk.PhotoImage(ImageGrab.grab(bbox=self.map_region).resize((270, 270)))
                self.canvas.delete('all')
                self.canvas.create_image(0, 0, image=self.preview, anchor='nw')
            if self.last_seen:
                x, y, stamp = self.last_seen
                age = now - stamp
                x, y = x * 270, y * 270
                if age < 45:
                    radius = (0.025 + age * self.expansion.get()) * 270
                    self.canvas.create_oval(x-radius, y-radius, x+radius, y+radius, outline='orange', width=3)
                    self.canvas.create_oval(x-4, y-4, x+4, y+4, fill='red')
                    self.jungle.set(f'Última aparição há {age:.0f}s. Incerteza crescente; não é posição confirmada.')
                else:
                    self.jungle.set(f'Aparição há {age:.0f}s: informação expirada. Marque uma nova aparição.')
        except Exception as exc:
            self.running = False
            self.status.set(f'Pausado: {exc}')
        self.root.after(650, self.tick)


if __name__ == '__main__':
    App()
