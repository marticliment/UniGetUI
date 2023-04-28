from enum import IntEnum, Enum


class AudioMode(IntEnum):
    """
    AudioMode is enumeration holds all valid parameters accepted by :py:meth:`zroya.Template.setAudio` method's `mode`
    parameter.

    Example:
        .. code-block:: python

            # t is an instance of zroya.Template
            t.setAudio( mode=zroya.AudioMode.Silence )

    """

    Default = 0
    """
    Selected audio will be played only once.
    """

    Silence = 1
    """
    No audio is played at all.
    """

    Loop = 2
    """
    Play audio in loop until it is moved to Action Center. This time may vary due to different user
    configuration.
    """


class Audio(Enum):
    """
    Audio enumeration contains values for  accepted values for `audio` parameter of :py:meth:`zroya.Template.setAudio`
    method.

    Example:
        .. code-block:: python

            # t is an instance of zroya.Template
            t.setAudio( audio=zroya.Audio.IM )

    """

    Default = "ms-winsoundevent:Notification.Default"
    """
    .. raw:: html
        
        <iframe src="_static/audio.html?file=Default"></iframe>

    """

    IM = "ms-winsoundevent:Notification.IM"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=IM"></iframe>

    """

    Mail = "ms-winsoundevent:Notification.Mail"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Email"></iframe>

    """

    Reminder = "ms-winsoundevent:Notification.Reminder"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Reminder"></iframe>

    """

    SMS = "ms-winsoundevent:Notification.SMS"
    Alarm = "ms-winsoundevent:Notification.Looping.Alarm"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm01&loop"></iframe>

    """

    Alarm2 = "ms-winsoundevent:Notification.Looping.Alarm2"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm02&loop"></iframe>

    """

    Alarm3 = "ms-winsoundevent:Notification.Looping.Alarm3"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm03&loop"></iframe>

    """

    Alarm4 = "ms-winsoundevent:Notification.Looping.Alarm4"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm04&loop"></iframe>

    """

    Alarm5 = "ms-winsoundevent:Notification.Looping.Alarm5"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm05&loop"></iframe>

    """

    Alarm6 = "ms-winsoundevent:Notification.Looping.Alarm6"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm06&loop"></iframe>

    """

    Alarm7 = "ms-winsoundevent:Notification.Looping.Alarm7"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm07&loop"></iframe>

    """

    Alarm8 = "ms-winsoundevent:Notification.Looping.Alarm8"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm08&loop"></iframe>

    """

    Alarm9 = "ms-winsoundevent:Notification.Looping.Alarm9"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm09&loop"></iframe>

    """

    Alarm10 = "ms-winsoundevent:Notification.Looping.Alarm10"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Alarm10&loop"></iframe>

    """

    Call = "ms-winsoundevent:Notification.Looping.Call"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring01&loop"></iframe>

    """

    Call2 = "ms-winsoundevent:Notification.Looping.Call2"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring02&loop"></iframe>

    """

    Call3 = "ms-winsoundevent:Notification.Looping.Call3"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring03&loop"></iframe>

    """

    Call4 = "ms-winsoundevent:Notification.Looping.Call4"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring04&loop"></iframe>

    """

    Call5 = "ms-winsoundevent:Notification.Looping.Call5"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring05&loop"></iframe>

    """

    Call6 = "ms-winsoundevent:Notification.Looping.Call6"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring06&loop"></iframe>

    """

    Call7 = "ms-winsoundevent:Notification.Looping.Call7"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring07&loop"></iframe>

    """

    Call8 = "ms-winsoundevent:Notification.Looping.Call8"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring08&loop"></iframe>

    """

    Call9 = "ms-winsoundevent:Notification.Looping.Call9"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring09&loop"></iframe>

    """

    Call10 = "ms-winsoundevent:Notification.Looping.Call10"
    """
    .. raw:: html

        <iframe src="_static/audio.html?file=Ring10&loop"></iframe>

    """


class TemplateType(IntEnum):
    """
    All possible values for :py:class:`zroya.Template` constructor.

    Example:
        .. code-block:: python

            zroya.Template(zroya.TemplateType.ImageAndText2)

    """

    ImageAndText1 = 0
    """
    A large image and a single string wrapped across three lines of text.
    """

    ImageAndText2 = 1
    """
    A large image, one string of bold text on the first line, one string of regular text wrapped across the
    second and third lines.
    """

    ImageAndText3 = 2
    """
    A large image, one string of bold text wrapped across the first two lines, one string of regular text on
    the third line.
    """

    ImageAndText4 = 3
    """
    A large image, one string of bold text on the first line, one string of regular text on the second line,
    one string of regular text on the third line.
    """

    Text1 = 4
    """
    Single string wrapped across three lines of text.
    """

    Text2 = 5
    """
    One string of bold text on the first line, one string of regular text wrapped across the second and third
    lines.
    """

    Text3 = 6
    """
    One string of bold text wrapped across the first two lines, one string of regular text on the third line.
    """

    Text4 = 7
    """
    One string of bold text on the first line, one string of regular text on the second line, one string of
    regular text on the third line.
    """
