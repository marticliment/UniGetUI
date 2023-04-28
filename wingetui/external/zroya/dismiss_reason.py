class DismissReason(object):

    """
    This class represents a notification dismiss reason. It is passed to callback registered in on_dismiss parameter
    of :py:func:`zroya.show` function.

    You can print it to get a reason description or compare it with any of following attributes.
    """

    User = 0
    """
    The user dismissed the toast.
    """

    App = 1
    """
    The application hid the toast using :py:func:`zroya.hide`.
    """

    Expired = 2
    """
    The toast has expired.
    """

    def __init__(self, reason):
        """
        Create an instance of dismiss reason. Zroya uses this class to return a dismiss reason of notification back to
        zroya callback. For instance, when you create a notification with on_dismiss callback set:

        .. code-block:: python

            def myDismissCallback(notificationID, reason):
                print("Notification {} was dismissed by user. Reason: {}.".format(notificationID, reason))

            # t is an instance of zroya.Template
            zroya.show(t, on_dismiss = myCallback)

        this class will be used as a second parameter 'reason'. Since this is kind of C->Python bridge class, you
        will probably never create an instance of it.

        Args:
            reason (int): Integer representation of C dismiss reason.
        """

        self._reason = reason

    def __str__(self):

        # Make sure, numbers corresponds with actual dismiss reasons from Windows core:
        # Search ToastDismissalReason::ToastDismissalReason_UserCanceled in wintoastlib.h

        if self._reason == DismissReason.User:
            return "The user dismissed the toast."
        if self._reason == DismissReason.App:
            return "The application hid the toast using zroya.hide."
        if self._reason == DismissReason.Expired:
            return "The toast has expired."

        return "Unknown dismiss reason. If you are seeing this, please report it as a bug. Thank you."

    def __eq__(self, other):
        if isinstance(other, int):
            return other == self._reason

        return False

    def __ne__(self, other):
        return not other == self
