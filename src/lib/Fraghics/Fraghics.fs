namespace Fraghics

open System.Drawing
open System.Windows.Forms

exception InvalidArgumentException

type G private () =

  static let (<?>) thing orElse =
    match thing with
    | None -> orElse
    | Some value -> value

  // form is backed by Bitmap, called formStore; formGraphics is the
  // corresponding Graphics. There is also backingStore and backGraphics.
  // 'autosynchonize' being true means that changes to backingStore are
  // immediately propagated to formStore; otherwise the changes from
  // backingStore will be drawn on Synchronize() call
  static let mutable form : Form = null
  static let mutable formStore : Bitmap = null
  static let mutable formGraphics : Graphics = null
  static let mutable backStore : Bitmap = null
  static let mutable backGraphics : Graphics = null
  static let mutable autosynchronize = true
  static let mutable background =
    new SolidBrush(Color.FromArgb(255, 255, 255, 255))
  static let mutable foreground =
    new SolidBrush(Color.FromArgb(255, 0, 0, 0))
  static let mutable pen = new Pen(foreground)
  static let mutable curx : int32 = 0
  static let mutable cury : int32 = 0

  static let createStoreAndGraphics w h (format: Imaging.PixelFormat) =
    let store = new Bitmap(w, h, format)
    let graphics = Graphics.FromImage(store)
    // TODO: graphics quality
    graphics.PixelOffsetMode = Drawing2D.PixelOffsetMode.HighQuality
      |> ignore
    graphics.Clear(background.Color)
    store, graphics

  static let newStores () =
    let rect = form.ClientRectangle
    let w = rect.Width
    let h = rect.Height
    let format = Imaging.PixelFormat.Format24bppRgb

    let fStore, fGraphics = createStoreAndGraphics w h format
    formGraphics <- fGraphics
    // Now let's copy old stuff on top of this one
    // If we just created a Window it may not exist, so check
    match formStore with
    | null -> ()
    | _ ->
      formGraphics.DrawImageUnscaled(formStore, 0, 0)
      // TODO: is it useful
      formStore.Dispose()
    formStore <- fStore

    let bStore, bGraphics = createStoreAndGraphics w h format
    backGraphics <- bGraphics
    // Copying old stuff again, see above
    match backStore with
    | null -> ()
    | _ ->
      backGraphics.DrawImageUnscaled(backStore, 0, 0)
      // TODO: is it useful
      backStore.Dispose()
    backStore <- bStore

  static let paint (evt : PaintEventArgs) =
    let rect = evt.ClipRectangle
    evt.Graphics.DrawImage(formStore, rect, rect, GraphicsUnit.Pixel)
    ()

  static let resize _ =
    newStores()
    form.Invalidate()
    Application.DoEvents()

  static member private Execute (draw: Graphics -> unit) =
    draw backGraphics
    if autosynchronize then
      draw formGraphics
      G.Synchronize()

  static member Synchronize () =
    formGraphics.DrawImageUnscaled(backStore, 0, 0)
    form.Invalidate()
    Application.DoEvents()

  static member OpenGraph(?width, ?height, ?title) =
    // Start with creating a form
    // TODO check if initialization is already done
    let text = title <?> "Fraghics"
    form <- new Form(Visible=true, Text=text)
    // TODO allow not to bind esc
    form.KeyDown.Add(fun e ->
      if e.KeyCode = Keys.Escape then
        form.Close()
        Application.Exit())
    let size = form.ClientSize
    let w = width <?> size.Width
    let h = height <?> size.Height
    form.ClientSize <- Size(w, h)
    newStores()
    form.Paint.Add(paint)
    form.Resize.Add(resize)

  static member CloseGraph() =
    // TODO check if it was open
    form.Close()
    // TODO is it useful?
    formStore.Dispose()
    backStore.Dispose()
    Application.Exit()

  static member SetWindowTitle title =
    // TODO: check that from exists
    // TODO: DOES NOT WORK
    failwith "Not implemented"
    form.Text = title |> ignore

  static member ResizeWindow width height =
    form.ClientSize <- Size(width, height)

  static member ClearGraph () =
    let rect = form.ClientRectangle
    let w = rect.Width
    let h = rect.Height
    (fun (g: Graphics) -> g.FillRectangle(background, 0, 0, w, h))
      |> G.Execute

  static member SizeX = form.ClientRectangle.Width
  static member SizeY = form.ClientRectangle.Height

  static member SetColor r g b =
    foreground <- new SolidBrush(Color.FromArgb(255, r, g, b))
    pen <- new Pen(foreground)

  static member SetColor1 color =
    foreground <- new SolidBrush(color)
    pen <- new Pen(foreground)

  static member private PlotImpl (x, y) =
    (fun (g: Graphics) -> g.FillRectangle(foreground, x, y, 1, 1))
      |> G.Execute

  static member Plot (x : int32) y =
    G.PlotImpl (x, y)
    G.MoveTo x y

  static member Plots points =
    // TODO should it be done in separate batches for form and backing store?
    Array.iter G.PlotImpl points
    let len = points.Length - 1
    let lastx, lasty = points.[len]
    G.MoveTo lastx lasty

  static member private LineToImpl (x, y) =
    (fun (g: Graphics) -> g.DrawLine(pen, curx, cury, x, y))
      |> G.Execute

  static member LineTo x y =
    G.LineToImpl(x, y)
    G.MoveTo x y

  static member RLineTo dx dy =
    let newx = curx + dx
    let newy = cury + dy
    G.LineToImpl(newx, newy)
    G.MoveTo newx newy

  // OCaml documentation says: curveto b c d draws a cubic Bezier curve
  // starting from the current point to point d, with control points b and c,
  // and moves the current point to d.
  static member CurveTo (c0x, c0y) (c1x, c1y) (dstx, dsty) =
    let start = Point(curx, cury)
    let cp0 = Point(c0x, c0y)
    let cp1 = Point(c1x, c1y)
    let dest = Point(dstx, dsty)
    (fun (g: Graphics) -> g.DrawBezier(pen, start, cp0, cp1, dest))
      |> G.Execute
    G.MoveTo dstx dsty

  static member DrawRect (x: int32) y w h =
    if w < 0 || h < 0 then raise InvalidArgumentException
    (fun (g: Graphics)-> g.DrawRectangle(pen, x, y, w, h))
      |> G.Execute


  // TODO think about color representations
  static member PointColor x y = formStore.GetPixel(x, y)

  static member MoveTo x y =
    curx <- x
    cury <- y

  static member RMoveto dx dy =
    curx <- curx + dx
    cury <- cury + dy

  static member CurrentX = curx
  static member CurrentY = cury
  static member CurrentPoint = curx, cury

  static member Autosynchronize
    with set(value) =
      autosynchronize <- value
      if value then G.Synchronize()

  static member Run() = Application.Run()



// vim: sw=2:sts=2:ai:foldmethod=indent:colorcolumn=80
